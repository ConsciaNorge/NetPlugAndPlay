using libterminal.JobRunner;
using System;

namespace libterminal.Tasks.Cisco
{
    public class PopBuffer : JobTask
    {
        public string Destination { get; set; }
        public string OnSuccess { get; set; }

        public override string Execute(Job job)
        {
            var destinationString = job.Value(Destination);
            var connection = ConnectionManager.Instance.ConnectionByUri(new Uri(destinationString));
            if (connection == null)
                throw new Exception(Name + " - No connection exists to destination");

            connection.PopBuffer();

            return OnSuccess;
        }
    }
}
