using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkDevicesPutViewModel
    {
        [JsonProperty("toAdd")]
        public List<NetworkDeviceAddViewModel> ToAdd { get; set; }
        [JsonProperty("toRemove")]
        public List<Guid> ToRemove { get; set; }
        [JsonProperty("toChange")]
        public List<NetworkDeviceChangeViewModel> ToChange { get; set; }
    }
}
