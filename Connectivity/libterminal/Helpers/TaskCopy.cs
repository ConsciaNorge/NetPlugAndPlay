using libterminal.JobRunner;
using libterminal.Tasks.Cisco.Compound;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libterminal.Helpers
{
    public class TaskCopy
    {
        public static string Run(Uri uri, string enablePassword, string source, string destination)
        {
            var runCiscoCommand = new Job
            {
                InitialTask = "RunCommand",
                Description = "Login to device and run a command",
                Parameters = new List<JobValue>
                {
                    new JobValue { Name = "Destination", Value = uri.ToString() },
                    new JobValue { Name = "Command", Value = "copy " + source + " " + destination },
                    new JobValue { Name = "OutputName", Value = "result" },
                },
                Tasks = new List<JobTask>
                {
                    new RunExecModeCommand("RunCommand", "Done", "Destination", enablePassword, "Command", "OutputName"),
                }
            };

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(runCiscoCommand));

            var runner = new JobRunner.JobRunner();
            var jobResult = runner.ExecuteJob(runCiscoCommand);

            if (jobResult == null || !jobResult.Success)
                return "";

            var result = jobResult.Values.Where(x => x.Name == "result").FirstOrDefault();
            if (result == null)
                return "";

            return result.Value;
        }
    }
}
