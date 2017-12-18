using Newtonsoft.Json;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class TemplateParameterAddViewModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
