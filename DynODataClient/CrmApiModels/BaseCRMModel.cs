using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynODataClient.CrmApiModels
{
    public class BaseCrmModel<T> where T : class
    {
        [JsonProperty("Error")]
        public Error CrmError { get; set; }
        [JsonProperty("@odata.context")]
        public string Context { get; set; }
        [JsonProperty("@odata.nextLink")]
        public string nextLink { get; set; }
        public IEnumerable<T> Value { get; set; }
        [JsonProperty("output")]
        public string Output { get; set; }
    }

    public class BaseBooleanResult
    {
        public BaseBooleanResult(bool result) => Result = result;
        public bool Result { get; set; } = false;
    }
}
