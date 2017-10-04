using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Models
{
    public class TemplateProperty
    {
        [Key]
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("templateConfiguration")]
        public virtual TemplateConfiguration TemplateConfiguration { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
