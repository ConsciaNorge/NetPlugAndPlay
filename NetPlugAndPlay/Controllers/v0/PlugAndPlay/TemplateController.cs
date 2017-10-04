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
    public class TemplateController : Controller
    {
        // GET: api/values
        [HttpGet]
        public async Task<List<Template>> Get(
                [FromServices] PnPServerContext dbContext
            )
        {
            return await dbContext.Templates.ToListAsync();
        }

        // GET api/values/5
        [HttpGet("{id}", Name = "GetTemplate")]
        public async Task<IActionResult> Get(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var item = await dbContext.Templates
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
                [FromBody] Template template
            )
        {
            if(template == null || string.IsNullOrEmpty(template.Name) || string.IsNullOrEmpty(template.Content) || template.Id != Guid.Empty)
            {
                return BadRequest();
            }

            var existingRecord = await dbContext.Templates.FirstOrDefaultAsync(x => x.Name == template.Name);
            if(existingRecord != null)
            {
                // TODO : Proper error for duplicate name record
                return BadRequest();
            }

            await dbContext.Templates.AddAsync(template);
            await dbContext.SaveChangesAsync();

            return new CreatedAtRouteResult("GetTemplate", new { id = template.Id }, template);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(
                [FromServices] PnPServerContext dbContext,
                Guid id,
                [FromBody] Template template
            )
        {
            if(template == null || template.Id != id)
            {
                return BadRequest();
            }

            var existingRecord = await dbContext.Templates.FirstOrDefaultAsync(x => x.Id == id);
            if(existingRecord == null)
            {
                return NotFound();
            }

            existingRecord.Name = template.Name;
            existingRecord.Content = template.Content;

            dbContext.Templates.Update(existingRecord);
            await dbContext.SaveChangesAsync();

            return new NoContentResult();
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var item = await dbContext.Templates.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound();

            dbContext.Templates.Remove(item);
            await dbContext.SaveChangesAsync();

            return new NoContentResult();
        }
    }
}
