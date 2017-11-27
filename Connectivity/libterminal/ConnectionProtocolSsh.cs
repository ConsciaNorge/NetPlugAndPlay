using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet.Common;

namespace libterminal
{
    public class ConnectionProtocolSsh : ConnectionProtocol
    {
        AuthenticationMethod _authenticationMethod;
        ConnectionInfo _connectionInfo;
        SshClient _client;
        ShellStream _stream;



        public ConnectionProtocolSsh(Connection connection) : base(connection)
        {

        }

        public override bool ConnectTo(Uri destination)
        {
            Destination = destination;

            var userInfoParts = destination.UserInfo.Split(':');
            var port = destination.Port == -1 ? 22 : destination.Port;

            _authenticationMethod = new PasswordAuthenticationMethod(userInfoParts[0], userInfoParts[1]);

            _connectionInfo = new ConnectionInfo(destination.Host, port, userInfoParts[0], _authenticationMethod);
            _client = new SshClient(_connectionInfo);
            try
            {
                _client.Connect();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                return false;
            }
            _stream = _client.CreateShellStream("xterm", 80, 25, 800, 600, 16384);
            _stream.DataReceived += _OnDataReceived;
            return true;
        }

        public override bool SendData(byte[] buffer)
        {
            try
            {
                _stream.Write(buffer, 0, buffer.Length);
                _stream.Flush();
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void _OnDataReceived(object sender, ShellDataEventArgs e)
        {
            if (OnReceived != null)
                OnReceived.Invoke(this, new DataReceivedEventArgs { Data = e.Data });
        }

        public override bool IsConnected {
            get
            {
                return _client != null && _client.IsConnected;
            }
        }
    }
}
