using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class BatchPutResultViewModel
    {
        [JsonProperty("changes")]
        public List<String> Changes { get; set; }
    }
}
