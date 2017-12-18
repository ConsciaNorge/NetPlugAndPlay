using Newtonsoft.Json;
using System.Collections.Generic;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class NetworkDeviceTemplateAddViewModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("parameters")]
        public List<TemplateParameterAddViewModel> Parameters { get; set; }
    }
}
