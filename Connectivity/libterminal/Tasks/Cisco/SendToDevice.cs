using libterminal.JobRunner;
using System;
using System.Text;

namespace libterminal.Tasks.Cisco
{
    public class SendToDevice : JobTask
    {
        public string Destination { get; set; }
        public string Message { get; set; }
        public string OnSuccess { get; set; }
        public string OnFailure { get; set; }
        public override string Execute(Job job)
        {
            var destinationString = job.Value(Destination);
            var connection = ConnectionManager.Instance.ConnectionByUri(new Uri(destinationString));
            if (connection == null)
                throw new Exception(Name + " - No connection exists to destination");

            connection.Mark();

            var processedMessage = job.Value(Message);

            if (!connection.SendData(Encoding.UTF8.GetBytes(processedMessage)))
                throw new Exception(Name + " - Failed to send message to : " + Destination);

            return OnSuccess;
        }
    }
}
