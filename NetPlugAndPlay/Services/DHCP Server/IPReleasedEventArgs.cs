using System;
using System.Net;

namespace NetPlugAndPlay.Services.DHCPServer
{
    public class IPReleasedEventArgs : EventArgs
    {
        public IPAddress Address;
    }
}