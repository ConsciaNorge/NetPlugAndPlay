using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class TemplateChangesViewModel
    {
        [JsonProperty("toAdd")]
        public List<TemplateAddViewModel> ToAdd { get; set; }

        [JsonProperty("toRemove")]
        public List<Guid> ToRemove { get; set; }

        [JsonProperty("toChange")]
        public List<TemplateChangeViewModel> ToChange { get; set; }
    }
}
