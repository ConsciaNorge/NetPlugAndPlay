using libterminal.Telnet;
using System;
using System.Collections.Generic;
using System.Text;

namespace libterminal
{
    public class ConnectionProtocolTelnet : ConnectionProtocol
    {
        TelnetClient _client;


        public ConnectionProtocolTelnet(Connection connection) : base(connection)
        {

        }

        public override bool ConnectTo(Uri destination)
        {
            Destination = destination;

            var port = destination.Port == -1 ? 23 : destination.Port;

            _client = new TelnetClient();
            _client.OnDataReceived += _OnDataReceived;
            try
            {
                _client.Connect(destination.Host, port);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                return false;
            }
            return true;
        }

        private void _OnDataReceived(object sender, DataReceived e)
        {
            if (OnReceived != null)
                OnReceived.Invoke(this, new DataReceivedEventArgs { Data = e.Data });
        }

        public override bool SendData(byte[] buffer)
        {
            try
            {
                _client.Write(buffer);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public override bool IsConnected
        {
            get
            {
                return _client != null && _client.IsConnected;
            }
        }
    }
}
