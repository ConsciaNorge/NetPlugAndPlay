using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPlugAndPlay.Models;
using Microsoft.EntityFrameworkCore;

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
    }
}
