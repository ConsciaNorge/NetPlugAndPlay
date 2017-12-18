using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkInterfaceChangeViewModel : NetworkInterfaceAddViewModel
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
    }
}
