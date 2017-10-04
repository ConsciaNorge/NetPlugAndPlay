using NetPlugAndPlay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.PlugAndPlay
{
    [Route("api/v0/PlugAndPlay/template/{templateId}/configuration")]
    public class TemplateConfigurationController : Controller
    {
        // GET: api/values
        [HttpGet]
        public async Task<List<TemplateConfiguration>> Get(
                [FromServices] PnPServerContext dbContext,
                Guid templateId
            )
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            var result = await dbContext.TemplateConfigurations
                .Include("NetworkDevice")
                .Include("Template")
                .Where(x =>
                    x.Template.Id == templateId
                )
                .ToListAsync();

            return result;
        }

        // GET api/values/5
        [HttpGet("{id}", Name = "GetTemplateConfiguration")]
        public async Task<IActionResult> Get(
                [FromServices] PnPServerContext dbContext,
                Guid templateId,
                Guid id
            )
        {
            var item = await dbContext.TemplateConfigurations
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
            if (item == null)
            {
                return NotFound();
            }

            return new ObjectResult(item);
        }

        public async Task<IActionResult> Post(
                [FromServices] PnPServerContext dbContext,
                Guid templateId,
                [FromBody] TemplateConfiguration templateConfiguration
            )
        {
            if (
                templateId == Guid.Empty ||
                templateConfiguration == null ||
                (
                    templateConfiguration.Template != null &&
                    templateConfiguration.Template.Id != Guid.Empty &&
                    templateConfiguration.Template.Id != templateId
                ) ||
                templateConfiguration.NetworkDevice == null ||
                templateConfiguration.NetworkDevice.Id == Guid.Empty
            )
            {
                System.Diagnostics.Debug.WriteLine("Invalid parameters passed");
                return BadRequest();
            }

            var template = await dbContext.Templates.Where(x => x.Id == templateId).FirstOrDefaultAsync();
            if(template == null)
            {
                System.Diagnostics.Debug.WriteLine("Invalid template id specified " + templateId.ToString());
                return NotFound();
            }

            var networkDevice = await dbContext.NetworkDevices.Where(x => x.Id == templateConfiguration.NetworkDevice.Id).FirstOrDefaultAsync();
            if(networkDevice == null)
            {
                System.Diagnostics.Debug.WriteLine("Invalid network device specified " + templateConfiguration.NetworkDevice.Id.ToString());
                return NotFound();
            }

            templateConfiguration.Template = template;
            templateConfiguration.NetworkDevice = networkDevice;

            await dbContext.TemplateConfigurations.AddAsync(templateConfiguration);
            await dbContext.SaveChangesAsync();

            // TODO : Better solution to handling cyclic reference
            for(var i=0; i<templateConfiguration.Properties.Count; i++)
            {
                templateConfiguration.Properties[i].TemplateConfiguration = null;
            }

            return new CreatedAtRouteResult("GetTemplateConfiguration", new { id = templateConfiguration.Id }, templateConfiguration);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
                [FromServices] PnPServerContext dbContext,
                Guid templateId,
                Guid id
            )
        {
            var item = await dbContext.TemplateConfigurations.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound();

            dbContext.TemplateConfigurations.Remove(item);
            await dbContext.SaveChangesAsync();

            return new NoContentResult();
        }
    }
}
