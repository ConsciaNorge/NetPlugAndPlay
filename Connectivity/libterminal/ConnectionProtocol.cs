using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace libterminal
{
    public class DataReceivedEventArgs : EventArgs
    {
        public byte [] Data { get; set; }
    }

    public abstract class ConnectionProtocol
    {
        public Uri Destination { get; set; }

        public Connection Connection { get; set; }

        public ConnectionProtocol(Connection connection)
        {
            Connection = connection;
        }

        public abstract bool ConnectTo(Uri destination);

        public abstract bool IsConnected { get; }

        public abstract bool SendData(byte[] buffer);

        public EventHandler<DataReceivedEventArgs> OnReceived = null;
    }
}
