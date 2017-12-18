using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class TemplateParameterChangesViewModel
    {
        [JsonProperty("toAdd")]
        public List<TemplateParameterAddViewModel> ToAdd { get; set; }
        [JsonProperty("toRemove")]
        public List<Guid> ToRemove { get; set; }
        [JsonProperty("toChange")]
        public List<TemplateParameterChangeViewModel> ToChange { get; set; }
    }
}
