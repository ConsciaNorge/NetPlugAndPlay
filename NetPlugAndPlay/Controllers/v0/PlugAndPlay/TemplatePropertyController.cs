using NetPlugAndPlay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.PlugAndPlay
{
    [Route("api/v0/PlugAndPlay/template/{templateId}/configuration/{configurationId}/property")]
    public class TemplatePropertyController : Controller
    {
        // GET: api/values
        [HttpGet]
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
    }
}
