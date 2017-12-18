using Newtonsoft.Json;
using System;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class DhcpExclusionChangeViewModel : DhcpExclusionAddViewModel
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
    }
}
