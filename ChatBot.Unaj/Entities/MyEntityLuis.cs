using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBot.Unaj.Entities
{
    public class MyEntityLuis
    {
        [JsonProperty("$instance")]
        public Instance _instance { get; set; }
        public List<List<string>> TipoConsulta { get; set; }
    }
    public class Instance
        {
            public List<TipoConsulta> TipoConsulta { get; set; }
        }
    public class TipoConsulta
    {
        public string type { get; set; }
        public string text { get; set; }
        public string modelType { get; set; }
    }
}
