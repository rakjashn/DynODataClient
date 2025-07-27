// =================================================================================
// File: ModelsAndHelpers.cs
// Description: Contains supporting models and static helpers for the library.
// =---------------------------------------------------------------------------------
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
namespace DynODataClient
{
    /// <summary>
    /// Represents the response for an individual operation within a bulk read ($batch) request.
    /// </summary>
    public class BatchReadResponse
    {
        public string ContentId { get; set; }
        public bool IsSuccess { get; set; }
        public string EntitySetName { get; set; }
        public List<JObject> Records { get; set; }
        public string ErrorJson { get; set; }
    }

    /// <summary>
    /// Contains static helper methods for OData operations.
    /// </summary>
    public static class ODataHelpers
    {
        /// <summary>
        /// A helper to extract the entity set name from the OData context URL.
        /// Example URL: "https://.../$metadata#mscrm_accounts(mscrm_accountid)"
        /// This will return "mscrm_accounts".
        /// </summary>
        public static string ParseEntitySetFromContextUrl(string contextUrl)
        {
            if (string.IsNullOrEmpty(contextUrl)) return null;

            const string metadataMarker = "#";
            const string openParen = "(";

            int metadataIndex = contextUrl.IndexOf(metadataMarker, StringComparison.Ordinal);
            if (metadataIndex == -1) return null;

            int parenIndex = contextUrl.IndexOf(openParen, metadataIndex, StringComparison.Ordinal);

            string entitySetName;
            if (parenIndex > -1)
            {
                entitySetName = contextUrl.Substring(metadataIndex + 1, parenIndex - (metadataIndex + 1));
            }
            else
            {
                entitySetName = contextUrl.Substring(metadataIndex + 1);
            }

            return entitySetName;
        }
    }
}
