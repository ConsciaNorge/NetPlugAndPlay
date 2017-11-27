using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetPlugAndPlay.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.TFTP
{
    [Produces("application/json")]
    [Route("api/v0/tftp/files")]
    public class Files : Controller
    {
        public class TFTPFileListResult
        {
            [JsonProperty("id")]
            public Guid Id { get; set; }
            [JsonProperty("filePath")]
            public string FilePath { get; set; }
            [JsonProperty("FileSize")]
            public long FileSize { get; set; }
        }

        // GET: api/values
        [HttpGet]
        public async Task<IActionResult> Get(
                [FromServices] PnPServerContext dbContext
            )
        {
            var filePath = Request.Query["filePath"];

            if (string.IsNullOrEmpty(filePath))
            {
                var result = await dbContext.TFTPFiles.Select(x => new TFTPFileListResult { Id = x.Id, FilePath = x.FilePath, FileSize = x.Content.Length }).ToListAsync();

                return new ObjectResult(result);
            }
            else
            {
                var result = await dbContext.TFTPFiles
                    .Where(x =>
                        x.FilePath == filePath
                    )
                    .FirstOrDefaultAsync();

                if (result == null)
                    return NotFound();

                return new ObjectResult(result);
            }
        }

        // GET: api/values
        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> Get(
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var result = await dbContext.TFTPFiles
                .Where(x =>
                    x.Id == id
                )
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound();

            return new ObjectResult(result);
        }

        // GET: api/values
        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> Delete (
                [FromServices] PnPServerContext dbContext,
                Guid id
            )
        {
            var result = await dbContext.TFTPFiles
                .Where(x =>
                    x.Id == id
                )
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound();

            dbContext.TFTPFiles.Remove(result);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        public class FileAddRequest
        {
            [JsonProperty("filePath")]
            public string FilePath { get; set; }
            [JsonProperty("content")]
            public string Content { get; set; }
        }

        // POST: api/v0/tftp/files
        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> Put(
            [FromServices] PnPServerContext dbContext,
            Guid id,
            [FromBody]FileAddRequest fileAddRequest
            )
        {
            var result = await dbContext.TFTPFiles
               .Where(x =>
                   x.Id == id
               )
               .FirstOrDefaultAsync();

            if (result == null)
                return NotFound();

            result.FilePath = fileAddRequest.FilePath;
            result.Content = fileAddRequest.Content;

            await dbContext.SaveChangesAsync();

            return new ObjectResult(result);
        }

        // POST: api/v0/tftp/files
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromServices] PnPServerContext dbContext,
            [FromBody]FileAddRequest fileAddRequest
            )
        {
            // TODO : Consider updating an existing record instead
            var result = new TFTPFile
            {
                FilePath = fileAddRequest.FilePath,
                Content = fileAddRequest.Content
            };

            await dbContext.AddAsync(result);
            await dbContext.SaveChangesAsync();

            return new ObjectResult(result);
        }
    }
}
