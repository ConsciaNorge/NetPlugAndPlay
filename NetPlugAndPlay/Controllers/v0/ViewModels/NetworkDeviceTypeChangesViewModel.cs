using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkDeviceTypeChangesViewModel
    {
        [JsonProperty("toAdd")]
        public List<NetworkDeviceTypeAddViewModel> ToAdd { get; set; }
        [JsonProperty("toRemove")]
        public List<Guid> ToRemove { get; set; }
        [JsonProperty("toChange")]
        public List<NetworkDeviceTypeChangeViewModel> ToChange { get; set; }
    }
}
