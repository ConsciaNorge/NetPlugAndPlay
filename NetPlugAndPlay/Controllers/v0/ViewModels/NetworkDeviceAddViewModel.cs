using Newtonsoft.Json;
using System.Collections.Generic;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkDeviceAddViewModel
    {
        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }
        [JsonProperty("hostname")]
        public string Hostname { get; set; }
        [JsonProperty("domainName")]
        public string DomainName { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("ipAddress")]
        public string IPAddress { get; set; }
        [JsonProperty("network")]
        public string Network { get; set; }
        [JsonProperty("dhcpRelay")]
        public bool DHCPRelay { get; set; }
        [JsonProperty("dhcpExclusions")]
        public List<DhcpExclusionAddViewModel> DHCPExclusions { get; set; }
        [JsonProperty("dhcpTftpBootfile")]
        public string DHCPTftpBootfile { get; set; }
    }
}
