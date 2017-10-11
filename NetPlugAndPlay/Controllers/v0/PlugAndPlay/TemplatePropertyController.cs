using NetPlugAndPlay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.PlugAndPlay
{
    //[Route("api/v0/PlugAndPlay/template/{templateId}/configuration/{configurationId}/property")]
    public class TemplatePropertyController : Controller
    {
        // GET: api/values
        [HttpGet("api/v0/PlugAndPlay/template/{templateId}/configuration/{configurationId}/property")]
        public async Task<List<TemplateProperty>> Get(
                [FromServices] PnPServerContext dbContext,
                Guid templateId,
                Guid configurationId
            )
        {
            return await dbContext.TemplateProperties
                .Where(x => 
                    x.TemplateConfiguration.Id == configurationId
                )
                .ToListAsync();
        }

        [HttpGet("api/v0/PlugAndPlay/template/property/{id}")]
        public async Task<IActionResult> GetById(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var item = await dbContext.TemplateProperties.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound();

            return new ObjectResult(item);
        }

        [HttpPost("api/v0/PlugAndPlay/template/configuration/{configurationId}/property")]
        public async Task<IActionResult> Post(
                [FromServices] PnPServerContext dbContext,
                Guid configurationId,
                [FromBody] TemplateProperty templateProperty
            )
        {
            var item = await dbContext.TemplateConfigurations.Include("Properties").FirstOrDefaultAsync(x => x.Id == configurationId);
            if (item == null)
                return NotFound();

            item.Properties.Add(new TemplateProperty
            {
                Name = templateProperty.Name,
                Value = templateProperty.Value
            });

            dbContext.TemplateConfigurations.Update(item);
            await dbContext.SaveChangesAsync();

            foreach(var property in item.Properties)
                property.TemplateConfiguration = null;

            return new ObjectResult(item);
        }

        [HttpPut("api/v0/PlugAndPlay/template/property/{id}")]
        public async Task<IActionResult> Put(
                [FromServices] PnPServerContext dbContext,
                Guid id,
                [FromBody] TemplateProperty templateProperty
            )
        {
            var item = await dbContext.TemplateProperties.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound();

            item.Name = templateProperty.Name;
            item.Value = templateProperty.Value;

            dbContext.TemplateProperties.Update(item);
            await dbContext.SaveChangesAsync();

            return new ObjectResult(item);
        }

        [HttpDelete("api/v0/PlugAndPlay/template/property/{id}")]
        public async Task<IActionResult> Delete(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var item = await dbContext.TemplateProperties.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound();

            dbContext.TemplateProperties.Remove(item);
            await dbContext.SaveChangesAsync();

            return new NoContentResult();
        }
    }
}
