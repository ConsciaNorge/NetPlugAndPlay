using LibDHCPServer.VolatilePool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.DeviceConfigurator.ViewModels
{
    public class DHCPPoolChangeViewModel
    {
        public bool ExistingDHCPRelay { get; set; }
        public NetworkPrefix ExistingPrefix { get; set; }

        public List<IPRange> ExistingReservations { get; set; }
        public bool DHCPRelay { get; set; }
        public NetworkPrefix Prefix { get; set; }
        public List<IPRange> Reservations { get; set; }
    }
}
