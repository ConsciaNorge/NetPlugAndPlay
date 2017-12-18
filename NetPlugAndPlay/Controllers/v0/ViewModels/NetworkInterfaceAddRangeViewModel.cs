using Newtonsoft.Json;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkInterfaceAddRangeViewModel
    {
        [JsonProperty("start")]
        public string Start { get; set; }
        [JsonProperty("firstIndex")]
        public int FirstIndex { get; set; }
        [JsonProperty("count")]
        public int Count { get; set; }
    }
}
