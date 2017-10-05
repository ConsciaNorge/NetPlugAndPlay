using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPlugAndPlay.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

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

            return await dbContext.NetworkDevices.Include("DeviceType").ToListAsync();
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
            if (networkDevice == null || networkDevice.DeviceType == null || networkDevice.DeviceType.Id == null)
            {
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
                System.Diagnostics.Debug.WriteLine("Network device with name " + networkDevice.Hostname + "." + networkDevice.DomainName + " already exists");
                return BadRequest();
            }

            var networkDeviceType = await dbContext.NetworkDeviceTypes.FindAsync(networkDevice.DeviceType.Id);
            if(networkDeviceType == null)
            {
                System.Diagnostics.Debug.WriteLine("Network device type " + networkDevice.DeviceType.Id.ToString() + " does not exist");
                return BadRequest();
            }

            networkDevice.DeviceType = networkDeviceType;

            await dbContext.NetworkDevices.AddAsync(networkDevice);
            await dbContext.SaveChangesAsync();

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
            var item = await dbContext.NetworkDevices.Where(x => x.Id == id).Include("DeviceType").FirstOrDefaultAsync();
            if (item == null)
                return NotFound();

            item.Hostname = networkDevice.Hostname;
            item.DomainName = networkDevice.DomainName;

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

            dbContext.NetworkDevices.Update(item);
            await dbContext.SaveChangesAsync();

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
    }
}
