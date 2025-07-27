// =================================================================================
// File: ODataAuthenticationHandler.cs
// Description: Contains the DelegatingHandlers for authentication.
// =---------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Text;

namespace DynODataClient
{/// <summary>
 /// A DelegatingHandler that adds a Basic Authentication header to outgoing requests.
 /// </summary>
    public class BasicAuthDelegatingHandler : DelegatingHandler
    {
        private readonly IOptions<BasicAuthOptions> _options;

        public BasicAuthDelegatingHandler(IOptions<BasicAuthOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(_options.Value?.Username))
            {
                throw new ArgumentException("Username is not configured in BasicAuthOptions.", nameof(options));
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var byteArray = Encoding.ASCII.GetBytes($"{_options.Value.Username}:{_options.Value.Password}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// A DelegatingHandler that acquires and attaches a bearer token for Dynamics 365.
    /// </summary>
    public class DynamicAuthHandler : DelegatingHandler
    {
        private readonly IOptions<DynamicAuthOptions> _options;
        private readonly ILogger<DynamicAuthHandler> _logger;
        private string _accessToken;
        private DateTimeOffset _tokenExpiryTime = DateTimeOffset.MinValue;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public DynamicAuthHandler(IOptions<DynamicAuthOptions> options, ILogger<DynamicAuthHandler> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrEmpty(_options.Value?.ClientID) ||
                string.IsNullOrEmpty(_options.Value?.ClientSecret) ||
                string.IsNullOrEmpty(_options.Value?.TenantID) ||
                string.IsNullOrEmpty(_options.Value?.ScopeUrl))
            {
                throw new ArgumentException("DynamicsAuthOptions is not configured properly.", nameof(options));
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiryTime)
            {
                return _accessToken;
            }

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiryTime)
                {
                    return _accessToken;
                }

                _logger.LogInformation("Access token is expired or not present. Acquiring new token.");

                var config = _options.Value;
                var clientApp = ConfidentialClientApplicationBuilder
                    .Create(config.ClientID)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();

                var scopes = new[] { config.ScopeUrl };

                AuthenticationResult result = await clientApp.AcquireTokenForClient(scopes)
                                                           .ExecuteAsync(cancellationToken);

                _accessToken = result.AccessToken;
                _tokenExpiryTime = result.ExpiresOn.AddMinutes(-5); // Add a 5-minute buffer

                _logger.LogInformation("Successfully acquired new access token.");

                return _accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire Dynamics 365 access token.");
                throw new InvalidOperationException("Failed to acquire Dynamics 365 access token.", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
