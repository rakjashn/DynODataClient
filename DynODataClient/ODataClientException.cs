// =================================================================================
// File: ODataClientException.cs
// Description: Custom exception for handling OData client errors.
// =---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DynODataClient
{
    /// <summary>
    /// Represents errors that occur during OData client operations.
    /// </summary>
    public class ODataClientException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ResponseContent { get; }

        public ODataClientException(string message, HttpStatusCode statusCode, string responseContent) : base(message)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }

        public ODataClientException(string message, HttpStatusCode statusCode, string responseContent, Exception inner) : base(message, inner)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }
}
