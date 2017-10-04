using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Models
{
    public class NetworkDeviceLink
    {
        [Key]
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("networkDevice")]
        public virtual NetworkDevice NetworkDevice { get; set; }

        [JsonProperty("interfaceIndex")]
        public int InterfaceIndex { get; set; }

        [JsonProperty("connectedToDevice")]
        public virtual NetworkDevice ConnectedToDevice { get; set; }

        [JsonProperty("connectedToInterfaceIndex")]
        public int ConnectedToInterfaceIndex { get; set; }
    }
}
