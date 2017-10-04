using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace NetPlugAndPlay.Models
{
    public class NetworkInterface
    {
        [Key]
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("deviceType")]
        public virtual NetworkDeviceType DeviceType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("interfaceIndex")]
        public int InterfaceIndex { get; set; }
    }
}
