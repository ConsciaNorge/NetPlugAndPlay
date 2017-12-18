using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class TFTPFileChangesViewModel
    {
        [JsonProperty("toAdd")]
        public List<TFTPFileAddViewModel> ToAdd { get; set; }
        [JsonProperty("toRemove")]
        public List<Guid> ToRemove { get; set; }
        [JsonProperty("toChange")]
        public List<TFTPFileChangeViewModel> ToChange { get; set; }
    }
}
