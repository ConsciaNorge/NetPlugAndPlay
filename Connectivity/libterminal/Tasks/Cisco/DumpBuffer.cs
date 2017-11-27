using libterminal.JobRunner;
using System;

namespace libterminal.Tasks.Cisco
{
    public class DumpBuffer : JobTask
    {
        public string Destination { get; set; }
        public string OnSuccess { get; set; }
        public override string Execute(Job job)
        {
            var destinationString = job.Value(Destination);
            var connection = ConnectionManager.Instance.ConnectionByUri(new Uri(destinationString));
            if (connection == null)
                throw new Exception(Name + " - No connection exists to destination");

            var value = connection.GetActiveBuffer();
            Console.WriteLine("Buffer" + value);

            return OnSuccess;
        }
    }
}
