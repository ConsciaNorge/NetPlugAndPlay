using libterminal.JobRunner;
using System;
using System.Text.RegularExpressions;

namespace libterminal.Tasks.Cisco
{
    public class MatchBuffer : JobTask
    {
        public string Destination { get; set; }
        public string Expression { get; set; }
        public string OnSuccess { get; set; }
        public string OnFailure { get; set; }
        public override string Execute(Job job)
        {
            var destinationString = job.Value(Destination);
            var connection = ConnectionManager.Instance.ConnectionByUri(new Uri(destinationString));
            if (connection == null)
                throw new Exception(Name + " - No connection exists to destination");

            var buffer = connection.GetActiveBuffer();
            var regex = new Regex(Expression);
            var match = regex.Match(buffer);
            if (match != null && match.Success)
                return OnSuccess;

            return OnFailure;
        }
    }
}
