using Newtonsoft.Json;
using System;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class TemplateParameterChangeViewModel : TemplateParameterAddViewModel
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
    }
}
