using System;
using System.Collections.Generic;
using System.Text;

namespace libterminal.Helpers.Model
{
    public enum ECDPCapability
    {
        Router,
        Switch,
        SourceRouteBridge,
        IGMP,
        TransBridge,
        CVTA,
        Phone,
        Port
    }

    public abstract class CDPExtra
    {
    }

    public class CDPNotImplementedYet : CDPExtra
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class CDPManagementIPAddresses : CDPExtra
    {
        public List<System.Net.IPAddress> ManagementIPAddresses { get; set; }
    }

    public class CDPVTPManagementDomain : CDPExtra
    {
        public string VTPManagementDomain { get; set; }
    }

    public class CDPDuplex : CDPExtra
    {
        public EDuplex Duplex { get; set; }
    }

    public class CDPNativeVLAN : CDPExtra
    {
        public int NativeVLAN { get; set; }
    }

    public class ShowCDPEntryItem
    {
        public string DeviceID { get; set; }
        public List<System.Net.IPAddress> EntryAddresses { get; set; }
        public string Platform { get; set; }
        public List<ECDPCapability> Capabilities { get; set; }
        public string Interface { get; set; }
        public string PortId { get; set; }
        public int HoldTime { get; set; }
        public string VersionText { get; set; }
        public int AdvertisementVersion { get; set; }
        public List<CDPExtra> AdvertisementExtras { get; set; }
    }
}