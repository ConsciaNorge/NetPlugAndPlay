using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Models
{
    public class NetworkDeviceType
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }
        [JsonProperty("productId")]
        public string ProductId { get; set; }
        [JsonProperty("interfaces")]
        public virtual List<NetworkInterface> Interfaces { get; set; }
    }
}
