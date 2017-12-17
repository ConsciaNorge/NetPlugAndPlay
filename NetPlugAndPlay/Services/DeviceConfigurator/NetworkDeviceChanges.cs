using NetPlugAndPlay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.DeviceConfigurator
{
    public class NetworkDeviceChanges
    {
        public NetworkDeviceChanges(NetworkDevice device)
        {
            Old = new NetworkDevice
            {
                DeviceType = device.DeviceType,
                Hostname = device.Hostname,
                DomainName = device.DomainName,
                Description = device.Description,
                IPAddress = device.IPAddress,
                Network = device.Network,
                DHCPRelay = device.DHCPRelay,
                DHCPExclusions = device.DHCPExclusions.Select(x => new DHCPExclusion { Id = x.Id, Start = x.Start, End = x.End }).ToList(),
                DHCPTftpBootfile = device.DHCPTftpBootfile
            };
        }

        public bool IsChanged
        {
            get
            {
                return (
                    Current == null ||
                    !Old.DeviceType.Id.Equals(Current.DeviceType.Id) ||
                    !Old.Hostname.Equals(Current.Hostname) ||
                    !Old.DomainName.Equals(Current.DomainName) ||
                    !Old.Description.Equals(Current.Description) ||
                    !Old.IPAddress.Equals(Current.IPAddress) ||
                    !Old.Network.Equals(Current.Network) ||
                    !Old.DHCPRelay.Equals(Current.DHCPRelay) ||
                    !ExclusionsEqual(Old.DHCPExclusions, Current.DHCPExclusions) ||
                    !Old.DHCPTftpBootfile.Equals(Current.DHCPTftpBootfile)
                );
            }
        }

        public NetworkDevice Old { get; set; }
        public NetworkDevice Current { get; set; }

        bool ExclusionsEqual(List<DHCPExclusion>a, List<DHCPExclusion>b)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            if (a.Count != b.Count)
                return false;

            foreach(var item in a)
            {
                if (b.Where(x => item.Start.Equals(x.Start) && item.End.Equals(x.End)).Count() != 1)
                    return false;
            }

            return true;
        }
    }
}
