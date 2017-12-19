using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkDeviceChangeViewModel
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
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
        public bool? DHCPRelay { get; set; }
        [JsonProperty("dhcpExclusions")]
        public DhcpExclusionChangesViewModel DHCPExclusions { get; set; }
        [JsonProperty("dhcpTftpBootfile")]
        public string DHCPTftpBootfile { get; set; }
        [JsonProperty("template")]
        public NetworkDeviceTemplateChangesViewModel Template { get; set; }
    }
}
