using NetPlugAndPlay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.PlugAndPlay
{
    [Route("api/v0/PlugAndPlay/[controller]")]
    public class NetworkDeviceTypeController : Controller
    {
        // GET: api/values
        [HttpGet]
        public async Task<List<NetworkDeviceType>> Get(
                [FromServices] PnPServerContext dbContext
            )
        {
            var name = Request.Query["name"];
            if(!string.IsNullOrEmpty(name))
                return await dbContext.NetworkDeviceTypes
                    .Where(x => x.Name == name)
                    .Include("Interfaces")
                    .ToListAsync();

            return await dbContext.NetworkDeviceTypes
                .Include("Interfaces")
                .ToListAsync();
        }

        // GET api/values/5
        [HttpGet("{id}", Name = "GetNetworkDeviceType")]
        public async Task<IActionResult> Get(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var item = await dbContext.NetworkDeviceTypes.FindAsync(id);
            if(item == null)
            {
                return NotFound();
            }

            return new ObjectResult(item);
        }

        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post(
                [FromServices] PnPServerContext dbContext,
                [FromBody] NetworkDeviceType networkDeviceType
            )
        {
            if(networkDeviceType == null)
            {
                return BadRequest();
            }

            var existingRecord = await dbContext.NetworkDeviceTypes
                .Where(x =>
                    x.Name.Equals(networkDeviceType.Name, StringComparison.OrdinalIgnoreCase)
                )
                .FirstOrDefaultAsync();

            if(existingRecord != null)
            {
                // TODO : Make it so bed request explains duplicate record
                return BadRequest();
            }

            await dbContext.NetworkDeviceTypes.AddAsync(networkDeviceType);
            await dbContext.SaveChangesAsync();

            return new CreatedAtRouteResult("GetNetworkDeviceType", new { id = networkDeviceType.Id }, networkDeviceType);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var item = await dbContext.NetworkDeviceTypes.FindAsync(id);
            if (item == null)
                return NotFound();

            dbContext.NetworkDeviceTypes.Remove(item);
            await dbContext.SaveChangesAsync();

            return new NoContentResult();
        }
    }
}
