using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bot_ApplicationBank.DataModels
{
    public class Timeline
    {
        [JsonProperty(PropertyName = "Id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "date")]
        public string  Date { get; set; }

        [JsonProperty(PropertyName = "transaction")]
        public string Transaction { get; set; }

    }
}