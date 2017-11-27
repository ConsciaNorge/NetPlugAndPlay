using libterminal.JobRunner;
using System;

namespace libterminal.Tasks.Cisco
{
    public class CiscoLoginTask : JobTask
    {
        public string Destination { get; set; }
        public string EnablePassword { get; set; }
        public string OnSuccess { get; set; }
        public string OnFailure { get; set; }

        public CiscoLoginTask()
        {

        }

        Connection EstablishConnection(Uri destination)
        {
            var connection = ConnectionManager.Instance.ConnectionByUri(destination);
            if (connection != null)
            {
                if (connection.IsConnected)
                    return connection;

                ConnectionManager.Instance.RemoveConnection(connection);
                connection = null;
            }

            connection = ConnectionManager.Instance.ConnectTo(destination);
            if (connection == null)
                throw new Exception("Failed to connect to destination " + Destination.ToString());

            return connection;
        }

        public override string Execute(Job job)
        {
            Connection connection = null;
            try
            {
                var destinationString = job.Value(Destination);
                connection = EstablishConnection(new Uri(destinationString));
                if (connection == null)
                    return OnFailure;
            }
            catch (Exception)
            {
                return OnFailure;
            }

            return OnSuccess;
        }
    }
}
