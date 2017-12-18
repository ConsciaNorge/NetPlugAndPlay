using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkConnectionsViewModel
    {
        [JsonProperty("toAdd")]
        public List<NetworkConnectionAddViewModel> ToAdd { get; set; }
        [JsonProperty("toRemove")]
        public List<Guid> ToRemove { get; set; }
    }
}
