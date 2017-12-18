using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkDeviceTemplateChangesViewModel
    {
        [JsonProperty("name")]
        public string Name{ get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("parameters")]
        public TemplateParameterChangesViewModel Parameters;
    }
}
