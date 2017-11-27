using libterminal.JobRunner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libterminal.Tasks.Common
{
    public class CompoundTask : JobTask
    {
        public string InitialTask { get; set; }
        public List<JobTask> Tasks { get; set; }
        public string OnSuccess { get; set; }

        public override string Execute(Job job)
        {
            System.Diagnostics.Debug.WriteLine("Fetching task -> " + Name + "." + InitialTask);
            var currentTask = Tasks.Where(x => x.Name == InitialTask).FirstOrDefault();
            if (currentTask == null)
                throw new Exception("Unknown task " + InitialTask + " specified but not found");

            while (true)
            {
                var processedDescription = job.Value(currentTask.Description);
                System.Diagnostics.Debug.WriteLine("Task description - " + processedDescription);

                string result = string.Empty;
                try
                {
                    result = currentTask.Execute(job);
                    System.Diagnostics.Debug.WriteLine(result);
                }
                catch
                {
                    result = "CriticalError";
                }

                switch (result)
                {
                    case "CriticalError":
                        return result;

                    case "Done":
                        return OnSuccess;

                    default:
                        System.Diagnostics.Debug.WriteLine("Fetching task -> " + Name + "." + result);
                        currentTask = Tasks.Where(x => x.Name == result).FirstOrDefault();
                        if (currentTask == null)
                            throw new Exception("Unknown task " + result + " specified but not found");

                        break;
                }
            }
        }
    }
}
