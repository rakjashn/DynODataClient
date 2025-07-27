// =================================================================================
// File: Options.cs
// Description: Contains configuration option classes for the library.
// =---------------------------------------------------------------------------------


namespace DynODataClient
{
    /// <summary>
    /// Common OData client options.
    /// </summary>
    public class ODataClientOptions
    {
        public string BaseUrl { get; set; }
        public string ODataUrlSuffix { get; set; } = "api/data/v9.2/";
    }

    /// <summary>
    /// Configuration options for Dynamics 365 (OAuth) authentication.
    /// </summary>
    public class DynamicAuthOptions : ODataClientOptions
    {
        public string TenantID { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string ScopeUrl { get; set; }
        public string Authority => $"https://login.microsoftonline.com/{TenantID}";
    }

    /// <summary>
    /// Configuration options for Basic Authentication.
    /// </summary>
    public class BasicAuthOptions : ODataClientOptions
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
