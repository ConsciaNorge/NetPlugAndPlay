using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetPlugAndPlay.Models;
using Serilog;
using NetPlugAndPlay.Controllers.v0.ViewModels;

namespace NetPlugAndPlay.Controllers.v0
{
    [Produces("application/json")]
    [Route("api/v0/batch")]
    public class BatchController : Controller
    {
        // PUT api/values/5
        [HttpPost()]
        public async Task<IActionResult> Put(
                [FromServices] PnPServerContext dbContext,
                Guid id,
                [FromBody] BatchPutViewModel changes
            )
        {
            Log.Logger.Here().Debug("PUT " + Url.ToString() + " called from " + HttpContext.Connection.RemoteIpAddress.ToString());



            return new ObjectResult(new BatchPutResultViewModel
            {
                Changes = new List<string>()
            });
        }
    }
}
