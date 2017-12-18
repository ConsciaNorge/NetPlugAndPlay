using Newtonsoft.Json;
using System;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class TemplateChangeViewModel
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}