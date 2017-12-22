using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetPlugAndPlay.Models;
using NetPlugAndPlay.Services.DeviceConfigurator;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NetPlugAndPlay.Controllers.v0.PlugAndPlay
{
    [Route("api/v0/PlugAndPlay/[controller]")]
    public class NetworkDeviceController : Controller
    {
        // GET: api/values
        [HttpGet]
        public async Task<List<NetworkDevice>> Get(
                [FromServices] PnPServerContext dbContext
            )
        {
            var hostname = Request.Query["hostname"];
            var domainName = Request.Query["domainName"];

            if (!string.IsNullOrEmpty(hostname) && !string.IsNullOrEmpty(domainName))
                return await dbContext.NetworkDevices.Where(x => x.Hostname == hostname && x.DomainName == domainName).Include("DeviceType").ToListAsync();
            else if (!string.IsNullOrEmpty(hostname))
                return await dbContext.NetworkDevices.Where(x => x.Hostname == hostname).Include("DeviceType").ToListAsync();
            else if (!string.IsNullOrEmpty(domainName))
                return await dbContext.NetworkDevices.Where(x => x.DomainName == domainName).Include("DeviceType").ToListAsync();

            return await dbContext.NetworkDevices
                .Include("DeviceType")
                .Include("Uplinks")
                .ToListAsync();
        }

        // GET api/values/5
        [HttpGet("{id}", Name = "GetNetworkDevice")]
        public async Task<IActionResult> Get(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var item = await dbContext.NetworkDevices
                .Include("DeviceType")
                .Include("Uplinks")
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
            if (item == null)
            {
                return NotFound();
            }

            return new ObjectResult(item);
        }

        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post(
                [FromServices] PnPServerContext dbContext,
                [FromBody] NetworkDevice networkDevice
            )
        {
            Log.Logger.Here().Debug("POST " + Url.ToString() + " called from " + HttpContext.Connection.RemoteIpAddress.ToString());

            if (networkDevice == null || networkDevice.DeviceType == null || networkDevice.DeviceType.Id == null)
            {
                Log.Logger.Here().Debug("POST " + Url.ToString() + " network device of invalid format");
                return BadRequest();
            }

            var existingRecord = await dbContext.NetworkDevices
                .Where(x =>
                    x.Hostname.Equals(networkDevice.Hostname, StringComparison.OrdinalIgnoreCase) &&
                    x.DomainName.Equals(networkDevice.DomainName, StringComparison.OrdinalIgnoreCase) 
                )
                .FirstOrDefaultAsync();

            if (existingRecord != null)
            {
                Log.Logger.Here().Error("Network device with name " + networkDevice.Hostname + "." + networkDevice.DomainName + " already exists");
                return BadRequest();
            }

            var networkDeviceType = await dbContext.NetworkDeviceTypes.FindAsync(networkDevice.DeviceType.Id);
            if(networkDeviceType == null)
            {
                Log.Logger.Here().Error("Network device type " + networkDevice.DeviceType.Id.ToString() + " does not exist");
                return BadRequest();
            }

            // TODO : Validate there are no duplicates in DHCP exclusions

            networkDevice.DeviceType = networkDeviceType;

            await dbContext.NetworkDevices.AddAsync(networkDevice);
            await dbContext.SaveChangesAsync();

            await DeviceConfigurator.NetworkDeviceAdded(networkDevice);

            return new CreatedAtRouteResult("GetNetworkDevice", new { id = networkDevice.Id }, networkDevice);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(
                [FromServices] PnPServerContext dbContext,
                Guid id,
                [FromBody] NetworkDevice networkDevice
            )
        {
            Log.Logger.Here().Debug("PUT " + Url.ToString() + " called from " + HttpContext.Connection.RemoteIpAddress.ToString());

            var item = await dbContext.NetworkDevices
                .Where(x => 
                    x.Id == id
                )
                .Include("DeviceType")
                .Include("DHCPExclusions")
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound();

            var changes = new NetworkDeviceChanges(item);

            item.Hostname = networkDevice.Hostname;
            item.DomainName = networkDevice.DomainName;

            // TODO : Validate there are no duplicates in DHCP exclusions

            if (item.DeviceType.Id != networkDevice.DeviceType.Id)
            {
                System.Diagnostics.Debug.WriteLine("TODO: If device type changes, force removal of all links if the new device doesn't have the same links as the old.");
                var deviceType = await dbContext.NetworkDeviceTypes.Where(x => x.Id == networkDevice.DeviceType.Id).FirstOrDefaultAsync();
                if (deviceType == null)
                    return NotFound();      // TODO : Better error for device type not found

                item.DeviceType = deviceType;
            }
            item.Description = networkDevice.Description;
            item.IPAddress = networkDevice.IPAddress;
            item.DHCPRelay = networkDevice.DHCPRelay;
            item.DHCPTftpBootfile = networkDevice.DHCPTftpBootfile;

            if (networkDevice.DHCPExclusions == null && item.DHCPExclusions != null)
            {
                Log.Logger.Here().Debug("Removing all DHCP exclusions from " + networkDevice.Hostname + "." + networkDevice.DomainName);
                item.DHCPExclusions.RemoveAll(x => true);
            }
            else if(networkDevice.DHCPExclusions != null)
            {
                if (item.DHCPExclusions == null)
                {
                    Log.Logger.Here().Debug(networkDevice.Hostname + "." + networkDevice.DomainName + " had no exclusions and now has some");
                    item.DHCPExclusions = networkDevice.DHCPExclusions;
                }
                else
                {
                    Log.Logger.Here().Debug(networkDevice.Hostname + "." + networkDevice.DomainName + " removing exclusions");
                    var removed = item.DHCPExclusions
                        .RemoveAll(x =>
                            networkDevice.DHCPExclusions
                                .Where(y =>
                                    y.Start.Equals(x.Start) &&
                                    y.End.Equals(x.End)
                                )
                                .Count() == 0
                        );
                    Log.Logger.Here().Debug(networkDevice.Hostname + "." + networkDevice.DomainName + " removed " + removed.ToString() + " exclusions");

                    Log.Logger.Here().Debug(networkDevice.Hostname + "." + networkDevice.DomainName + " adding new exclusions");
                    var addedItems = networkDevice.DHCPExclusions
                        .Where(x =>
                            item.DHCPExclusions
                                .Where(y =>
                                    y.Start.Equals(x.Start) &&
                                    y.End.Equals(x.End)
                                )
                                .Count() == 0
                        )
                        .ToList();

                    Log.Logger.Here().Debug(networkDevice.Hostname + "." + networkDevice.DomainName + " found " + addedItems.Count + " item to add");

                    dbContext.AddRange(addedItems);
                    item.DHCPExclusions.AddRange(addedItems);

                    Log.Logger.Here().Debug(networkDevice.Hostname + "." + networkDevice.DomainName + " added " + addedItems.Count + " items");
                }
            }

            dbContext.NetworkDevices.Update(item);
            await dbContext.SaveChangesAsync();

            changes.Current = item;
            if (changes.IsChanged)
                await DeviceConfigurator.NetworkDeviceChanged(changes);

            return new ObjectResult(item);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var item = await dbContext.NetworkDevices.FindAsync(id);
            if (item == null)
                return NotFound();

            dbContext.NetworkDevices.Remove(item);
            await dbContext.SaveChangesAsync();

            return new NoContentResult();
        }

        [HttpGet("{id}/configuration")]
        public async Task<IActionResult> TestConfiguration(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var item = await dbContext.NetworkDevices.FindAsync(id);
            if (item == null)
                return NotFound();

            var config = await Services.ConfigurationGenerator.Generator.Generate(item.IPAddress, dbContext);

            return new ObjectResult(new { text = config });
        }

        public class NetworkDeviceTemplateViewModel
        {
            [JsonProperty("id")]
            public Guid Id { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("description")]
            public string Description { get; set; }
            [JsonProperty("parameters")]
            public virtual List<TemplateProperty> Parameters { get; set; }
        }

        public class NetworkDeviceReportViewModel : NetworkDevice
        {
            [JsonProperty("template")]
            public NetworkDeviceTemplateViewModel Template { get; set; }
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetNetworkDeviceReport(
                [FromServices] PnPServerContext dbContext
            )
        {
            var result = await (
                    from networkDevice in dbContext.NetworkDevices
                    join matchingTemplate in dbContext.TemplateConfigurations
                    on networkDevice.Id equals matchingTemplate.NetworkDevice.Id
                    into joinTable
                    from template in joinTable.DefaultIfEmpty()
                    select new NetworkDeviceReportViewModel
                    {
                        Id = networkDevice.Id,
                        DeviceType = networkDevice.DeviceType,
                        Hostname  = networkDevice.Hostname,
                        DomainName  = networkDevice.DomainName,
                        Description = networkDevice.Description,
                        IPAddress = networkDevice.IPAddress,
                        Network = networkDevice.Network,
                        Uplinks = networkDevice.Uplinks,
                        DHCPRelay = networkDevice.DHCPRelay,
                        DHCPExclusions = networkDevice.DHCPExclusions,
                        DHCPTftpBootfile = networkDevice.DHCPTftpBootfile,
                        Template = (template == null) ? null :
                            new NetworkDeviceTemplateViewModel
                            {
                                Id = template.Template.Id,
                                Name = template.Template.Name,
                                Description = template.Description,
                                Parameters = template.Properties
                            }
                    }
                )
                .ToListAsync();

            return new ObjectResult(result);
        }

        [HttpGet("{id}/template")]
        public async Task<IActionResult> GetTemplateConfiguration(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var templateConfigurations = await dbContext.TemplateConfigurations
                .Include("Template")
                .Include("NetworkDevice")
                .Include("Properties")
                .Where(x => x.NetworkDevice.Id == id)
                .ToListAsync();

            foreach(var config in templateConfigurations)
            {
                foreach(var templateProperty in config.Properties)
                {
                    templateProperty.TemplateConfiguration = null;
                }
            }

            return new ObjectResult(templateConfigurations);
        }

        public class NetworkUplinkViewModel
        {
            [JsonProperty("id")]
            public Guid Id { get; set; }
            [JsonProperty("domainName")]
            public string DomainName { get; set; }
            [JsonProperty("networkDeviceId")]
            public Guid NetworkDeviceId { get; set; }
            [JsonProperty("networkDevice")]
            public string NetworkDevice { get; set; }
            [JsonProperty("interfaceIndex")]
            public int InterfaceIndex { get; set; }
            [JsonProperty("interface")]
            public string Interface { get; set; }
            [JsonProperty("uplinkToDeviceId")]
            public Guid UplinkToDeviceId { get; set; }
            [JsonProperty("uplinkToDevice")]
            public string UplinkToDevice { get; set; }
            [JsonProperty("uplinkToInterfaceIndex")]
            public int UplinkToInterfaceIndex { get; set; }
            [JsonProperty("uplinkToInterface")]
            public string UplinkToInterface { get; set; }
        }

        // GET api/values/5
        [HttpGet("uplinks")]
        public async Task<IActionResult> GetUplinks(
                [FromServices] PnPServerContext dbContext
            )
        {
            var uplinks = await dbContext.NetworkDeviceLinks
                .Include("NetworkDevice.DeviceType.Interfaces")
                .Include("ConnectedToDevice.DeviceType.Interfaces")
                .Include("ConnectedToDevice")
                .ToListAsync();

            if (uplinks == null)
            {
                return NotFound();
            }

            var result = new List<NetworkUplinkViewModel>();

            foreach(var uplink in uplinks)
            {
                try
                {
                    result.Add(
                        new NetworkUplinkViewModel
                        {
                            Id = uplink.Id,
                            DomainName = uplink.NetworkDevice.DomainName,
                            NetworkDeviceId = uplink.NetworkDevice.Id,
                            NetworkDevice = uplink.NetworkDevice.Hostname,
                            InterfaceIndex = uplink.InterfaceIndex,
                            Interface = uplink.NetworkDevice.DeviceType.Interfaces.Where(y => y.InterfaceIndex == uplink.InterfaceIndex).First().Name,
                            UplinkToDeviceId = uplink.ConnectedToDevice.Id,
                            UplinkToDevice = uplink.ConnectedToDevice.Hostname,
                            UplinkToInterfaceIndex = uplink.ConnectedToInterfaceIndex,
                            UplinkToInterface = uplink.ConnectedToDevice.DeviceType.Interfaces.Where(y => y.InterfaceIndex == uplink.ConnectedToInterfaceIndex).First().Name
                        }
                    );
                }
                catch(Exception e)
                {
                    Log.Logger.Here().Error(e, "Failed to find components of a network uplink");
                }
            }

            return new ObjectResult(result);
        }

        // GET api/values/5
        [HttpPost("uplinks/bydeviceids")]
        public async Task<IActionResult> GetUplinksByDeviceIds(
                [FromServices] PnPServerContext dbContext,
                [FromBody] List<Guid> deviceIds
            )
        {
            var uplinks = await dbContext.NetworkDeviceLinks
                .Where(x =>
                    deviceIds.Contains(x.NetworkDevice.Id) ||
                    deviceIds.Contains(x.ConnectedToDevice.Id)
                )
                .Include("NetworkDevice.DeviceType.Interfaces")
                .Include("ConnectedToDevice.DeviceType.Interfaces")
                .Include("ConnectedToDevice")
                .ToListAsync();

            if (uplinks == null)
            {
                return NotFound();
            }

            var result = new List<NetworkUplinkViewModel>();

            foreach (var uplink in uplinks)
            {
                try
                {
                    result.Add(
                        new NetworkUplinkViewModel
                        {
                            Id = uplink.Id,
                            DomainName = uplink.NetworkDevice.DomainName,
                            NetworkDeviceId = uplink.NetworkDevice.Id,
                            NetworkDevice = uplink.NetworkDevice.Hostname,
                            InterfaceIndex = uplink.InterfaceIndex,
                            Interface = uplink.NetworkDevice.DeviceType.Interfaces.Where(y => y.InterfaceIndex == uplink.InterfaceIndex).First().Name,
                            UplinkToDeviceId = uplink.ConnectedToDevice.Id,
                            UplinkToDevice = uplink.ConnectedToDevice.Hostname,
                            UplinkToInterfaceIndex = uplink.ConnectedToInterfaceIndex,
                            UplinkToInterface = uplink.ConnectedToDevice.DeviceType.Interfaces.Where(y => y.InterfaceIndex == uplink.ConnectedToInterfaceIndex).First().Name
                        }
                    );
                }
                catch (Exception e)
                {
                    Log.Logger.Here().Error(e, "Failed to find components of a network uplink");
                }
            }

            return new ObjectResult(result);
        }
    }
}
