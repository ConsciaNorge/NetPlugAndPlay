using Newtonsoft.Json;
using System;

namespace NetPlugAndPlay.Models
{
    public class DHCPExclusion
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
        [JsonProperty("start")]
        public string Start { get; set; }
        [JsonProperty("end")]
        public string End { get; set; }
    }
}
