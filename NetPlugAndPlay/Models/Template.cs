using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Models
{
    public class Template
    {
        [Key]
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [MaxLength]
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
