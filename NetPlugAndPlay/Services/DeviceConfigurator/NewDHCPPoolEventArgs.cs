using LibDHCPServer.VolatilePool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.DeviceConfigurator
{
    public class NewDHCPPoolEventArgs : EventArgs
    {
        public DhcpPool Pool { get; set; }
    }
}
