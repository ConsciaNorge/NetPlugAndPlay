using LibDHCPServer.HardwareAddressTypes;
using LibDHCPServer.VolatilePool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.DHCP_Server
{
    public class DHCPClient
    {
        public ClientHardwareAddress ClientId { get; set; }
        public Guid NetworkDeviceId { get; set; }
        public Lease DHCPLease { get; set; }
    }
}
