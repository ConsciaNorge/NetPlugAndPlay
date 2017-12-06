using System;
using System.Net;

namespace NetPlugAndPlay.Services.DHCP_Server
{
    public class IPReleasedEventArgs : EventArgs
    {
        public IPAddress Address;
    }
}