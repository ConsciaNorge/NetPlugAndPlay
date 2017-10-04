using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Models
{
    public class TemplateConfiguration
    {
        [Key]
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("template")]
        public virtual Template Template { get; set; }

        [JsonProperty("networkDevice")]
        public virtual NetworkDevice NetworkDevice { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("properties")]
        public virtual List<TemplateProperty> Properties { get; set; }
    }
}
