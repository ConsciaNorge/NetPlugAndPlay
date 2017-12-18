using Newtonsoft.Json;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkConnectionAddViewModel
    {
        [JsonProperty("domainName")]
        public string DomainName { get; set; }
        [JsonProperty("networkDevice")]
        public string NetworkDevice { get; set; }
        [JsonProperty("interface")]
        public string Interface { get; set; }
        [JsonProperty("uplinkToDevice")]
        public string UplinkToDevice { get; set; }
        [JsonProperty("uplinktoInterface")]
        public string UplinkToInterface { get; set; }
    }
}