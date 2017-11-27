using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using libterminal.JobRunner;
using libterminal.Tasks.Cisco.Compound;

namespace NetPlugAndPlay.Controllers.v0.Terminal.Cisco
{
    [Produces("application/json")]
    [Route("api/v0/terminal/cisco/exec")]
    public class ExecController : Controller
    {
        public class ExecCommandRequest
        {
            [JsonProperty("description")]
            public string Description { get; set; }
            [JsonProperty("command")]
            public string Command { get; set; }
            [JsonProperty("destination")]
            public string Destination { get; set; }
            [JsonProperty("enablePassword")]
            public string EnablePasssword { get; set; }
        }

        // POST: api/v0/terminal/cisco/exec
        [HttpPost]
        public IActionResult Post([FromBody]ExecCommandRequest commandRequest)
        {
            var runCiscoCommand = new Job
            {
                InitialTask = "RunCommand",
                Description = "Login to device and run a command",
                Parameters = new List<JobValue>
                {
                    new JobValue { Name = "Destination", Value = commandRequest.Destination },
                    new JobValue { Name = "Command", Value = commandRequest.Command },
                    new JobValue { Name = "OutputName", Value = "result" },
                },
                Tasks = new List<JobTask>
                {
                    new RunExecModeCommand("RunCommand", "Done", "Destination", commandRequest.EnablePasssword, "Command", "OutputName"),
                }
            };

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(runCiscoCommand));

            var runner = new JobRunner();
            var jobResult = runner.ExecuteJob(runCiscoCommand);

            if (jobResult == null || !jobResult.Success)
                return NotFound();

            var result = jobResult.Values.Where(x => x.Name == "result").FirstOrDefault();
            if (result == null)
                return BadRequest();

            return new ObjectResult(result.Value);
        }
    }
}