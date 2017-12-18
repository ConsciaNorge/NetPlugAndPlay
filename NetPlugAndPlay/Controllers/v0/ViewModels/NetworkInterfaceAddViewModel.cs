using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkInterfaceAddViewModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("interfaceIndex")]
        public int InterfaceIndex { get; set; }
    }
}
