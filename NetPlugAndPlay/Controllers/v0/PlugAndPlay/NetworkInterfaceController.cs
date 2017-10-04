using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPlugAndPlay.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NetPlugAndPlay.PlugAndPlayTools.Cisco;

namespace NetPlugAndPlay.Controllers.v0.PlugAndPlay
{
    [Route("api/v0/PlugAndPlay/networkDeviceType/{networkDeviceTypeId:Guid}/interface")]
    public class NetworkInterfaceController : Controller
    {
        // GET: api/values
        [HttpGet(Name = "GetNetworkInterfaces")]
        public async Task<List<NetworkInterface>> Get(
                [FromServices] PnPServerContext dbContext,
                Guid networkDeviceTypeId
            )
        {
            return await dbContext.NetworkInterfaces.Where(x => x.DeviceType.Id == networkDeviceTypeId).ToListAsync();
        }

        // GET api/values/5
        [HttpGet("{id}", Name = "GetNetworkInterface")]
        public async Task<IActionResult> Get(
                [FromServices] PnPServerContext dbContext,
                Guid networkDeviceTypeId,
                Guid id
            )
        {
            var item = await dbContext.NetworkInterfaces
                .Include("DeviceType")
                .Where(
                    x => 
                        x.Id == id &&
                        x.DeviceType.Id == networkDeviceTypeId
                )
                .FirstOrDefaultAsync();
            if (item == null)
            {
                return NotFound();
            }

            // TODO : Come up with a better solution for coping with cyclic references between interface and device type
            item.DeviceType.Interfaces = null;

            return new ObjectResult(item);
        }

        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post(
                [FromServices] PnPServerContext dbContext,
                Guid networkDeviceTypeId,
                [FromBody] NetworkInterface networkInterface
            )
        {
            if(networkDeviceTypeId == Guid.Empty ||
               networkInterface == null || 
               string.IsNullOrEmpty(networkInterface.Name) || 
               (networkInterface.DeviceType != null && networkInterface.DeviceType.Id != networkDeviceTypeId)
               )
            {
                return BadRequest();
            }

            var networkDeviceType = await dbContext.NetworkDeviceTypes.Include("Interfaces").FirstOrDefaultAsync(x => x.Id == networkDeviceTypeId);
            if(networkDeviceType  == null)
            {
                System.Diagnostics.Debug.WriteLine("Network device type " + networkDeviceTypeId.ToString() + " does not exist");
                return BadRequest();
            }

            var existingRecord = await dbContext.NetworkInterfaces
                .FirstOrDefaultAsync(x => 
                    x.Name == networkInterface.Name &&
                    x.DeviceType.Id == networkDeviceTypeId
                 );

            if (existingRecord != null)
            {
                System.Diagnostics.Debug.WriteLine("Network interface " + networkInterface.Name + " for device type " + networkDeviceType.Name + " already exists");
                return BadRequest();
            }

            networkDeviceType.Interfaces.Add(networkInterface);
            dbContext.Update(networkDeviceType);

            await dbContext.SaveChangesAsync();

            // TODO : Come up with a better solution for coping with cyclic references between interface and device type
            networkInterface.DeviceType.Interfaces = null;

            return new CreatedAtRouteResult("GetNetworkInterface", new { networkDeviceTypeId = networkDeviceTypeId, id = networkInterface.Id }, networkInterface);
        }

        public class PostNetworkInterfaceRangeViewModel
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("firstIndex")]
            public int FirstIndex { get; set; }
            [JsonProperty("count")]
            public int Count { get; set; }
        }

        // POST api/values
        [HttpPost("range")]
        public async Task<IActionResult> PostRange(
                [FromServices] PnPServerContext dbContext,
                Guid networkDeviceTypeId,
                [FromBody] PostNetworkInterfaceRangeViewModel range
            )
        {
            // TODO : Additional verification on range validity
            if (networkDeviceTypeId == Guid.Empty ||
               range == null ||
               string.IsNullOrEmpty(range.Name) 
               )
            {
                return BadRequest();
            }

            var interfaceName = InterfaceName.tryParse(range.Name);
            if (interfaceName == null)
            {
                System.Diagnostics.Debug.WriteLine("Invalid format for interface name : " + range.Name);
                return BadRequest();
            }

            var networkDeviceType = await dbContext.NetworkDeviceTypes.Include("Interfaces").FirstOrDefaultAsync(x => x.Id == networkDeviceTypeId);
            if (networkDeviceType == null)
            {
                System.Diagnostics.Debug.WriteLine("Network device type " + networkDeviceTypeId.ToString() + " does not exist");
                return BadRequest();
            }

            var interfaceList = new List<string>();
            var networkInterfaceRecordList = new List<NetworkInterface>();
            for(var i=0; i<range.Count; i++)
            {
                var newInterfaceName = interfaceName.subsequent(i).ToString();
                interfaceList.Add(newInterfaceName);
                networkInterfaceRecordList.Add(new NetworkInterface
                {
                    Name = newInterfaceName,
                    InterfaceIndex = range.FirstIndex + i
                });
            }

            var conflictingRecords = await dbContext.NetworkInterfaces
                .Where(x =>
                    interfaceList.Contains(x.Name, StringComparer.OrdinalIgnoreCase) &&
                    x.DeviceType.Id == networkDeviceTypeId
                )
                .ToListAsync();

            if(conflictingRecords.Count() != 0)
            {
                System.Diagnostics.Debug.WriteLine("Conflicting network interface names found");
                return BadRequest();
            }

            networkDeviceType.Interfaces.AddRange(networkInterfaceRecordList);
            dbContext.Update(networkDeviceType);
            
            await dbContext.SaveChangesAsync();

            foreach(var networkInterface in networkInterfaceRecordList)
            {
                networkInterface.DeviceType.Interfaces = null;
            }

            return new CreatedAtRouteResult("GetNetworkInterfaces", networkInterfaceRecordList);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
                [FromServices] PnPServerContext dbContext,
                Guid networkDeviceTypeId,
                Guid id
            )
        {
            var item = await dbContext.NetworkInterfaces.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound();

            dbContext.NetworkInterfaces.Remove(item);
            await dbContext.SaveChangesAsync();

            return new NoContentResult();
        }
    }
}
