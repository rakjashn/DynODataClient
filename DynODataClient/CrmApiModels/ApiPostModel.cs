using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynODataClient.CrmApiModels
{
    public class ApiPostResponse
    {
        //private string _error;
        //private bool _status;
        public Uri LocationPath { get; set; }
        public string EntityId { get; set; }
        //public bool Status { get { return _status; } set { _status = error == null; } }
        public bool Status { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public Error error { get; set; }
    }

    public class Error
    {
        public string code { get; set; }
        public string message { get; set; }
    }
}
