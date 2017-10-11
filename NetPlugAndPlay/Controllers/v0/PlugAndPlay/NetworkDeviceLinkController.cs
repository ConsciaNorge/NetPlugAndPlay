using NetPlugAndPlay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.PlugAndPlay
{
    [Route("api/v0/PlugAndPlay/networkdevice/{networkDeviceId:Guid}/uplink")]
    public class NetworkDeviceLinkController : Controller
    {
        // GET: api/values
        [HttpGet]
        public async Task<List<NetworkDeviceLink>> Get(
                [FromServices] PnPServerContext dbContext,
                Guid networkDeviceId
            )
        {
            // TODO : Make sure to get network device and connected to device from the database
            var result = await dbContext.NetworkDeviceLinks
                .Where(x =>
                    x.NetworkDevice.Id == networkDeviceId
                )
                .Include("NetworkDevice")
                .Include("ConnectedToDevice")
                .ToListAsync();

            foreach(var device in result)
            {
                device.NetworkDevice.Uplinks = null;
                device.ConnectedToDevice.Uplinks = null;
            }

            return result;
        }

        // GET api/values/5
        [HttpGet("{id}", Name = "GetNetworkDeviceLink")]
        public async Task<IActionResult> Get(
                [FromServices] PnPServerContext dbContext,
                Guid networkDeviceId,
                Guid id
            )
        {
            var item = await dbContext.NetworkDeviceLinks
                .Where(x => 
                    x.Id == id 
                )
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
                [FromBody] NetworkDeviceLink networkDeviceLink
            )
        {
            if(
                networkDeviceLink == null ||
                networkDeviceLink.Id != Guid.Empty ||
                networkDeviceLink.NetworkDevice == null ||
                networkDeviceLink.NetworkDevice.Id == Guid.Empty ||
                networkDeviceLink.ConnectedToDevice == null ||
                networkDeviceLink.ConnectedToDevice.Id == Guid.Empty 
            )
            {
                return BadRequest();
            }

            var networkDevice = await dbContext.NetworkDevices
                .Include("Uplinks")
                .Where(x => x.Id == networkDeviceLink.NetworkDevice.Id)
                .FirstOrDefaultAsync();

            if(networkDevice == null)
            {
                System.Diagnostics.Debug.WriteLine("Invalid network device " + networkDeviceLink.NetworkDevice.Id.ToString());
                return NotFound();
            }

            var connectedToDevice = await dbContext.NetworkDevices
                .Where(x => x.Id == networkDeviceLink.ConnectedToDevice.Id)
                .FirstOrDefaultAsync();

            if(connectedToDevice == null)
            {
                System.Diagnostics.Debug.WriteLine("Invalid connected to network device " + networkDeviceLink.NetworkDevice.Id.ToString());
                return NotFound();
            }

            // TODO : Check other links to see if the source or destination ports are in use elsewhere

            var newLink = new NetworkDeviceLink
            {
                InterfaceIndex = networkDeviceLink.InterfaceIndex,
                ConnectedToDevice = connectedToDevice,
                ConnectedToInterfaceIndex = networkDeviceLink.ConnectedToInterfaceIndex
            };

            networkDevice.Uplinks.Add(newLink);

            dbContext.NetworkDevices.Update(networkDevice);
            await dbContext.SaveChangesAsync();

            newLink.NetworkDevice.Uplinks = null;

            return new CreatedAtRouteResult("GetNetworkDeviceLink", new { id = newLink.Id }, newLink);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
                [FromServices] PnPServerContext dbContext,
                Guid networkDeviceId,
                Guid id
            )
        {
            var item = await dbContext.NetworkDeviceLinks.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound();

            dbContext.NetworkDeviceLinks.Remove(item);
            await dbContext.SaveChangesAsync();

            return new NoContentResult();
        }
    }
}
