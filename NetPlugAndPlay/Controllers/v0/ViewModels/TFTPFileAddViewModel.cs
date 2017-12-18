using Newtonsoft.Json;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class TFTPFileAddViewModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}