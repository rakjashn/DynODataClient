// =================================================================================
// File: ODataClient.cs
// Description: The main client for interacting with an OData endpoint.
//              Refactored to be stateless and more robust.
// =---------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DynODataClient
{
    /// <summary>
    /// A client for making requests to an OData v4 API.
    /// </summary>
    public class ODataClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ODataClient> _logger;
        private readonly ODataClientOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataClient"/> class.
        /// </summary>
        public ODataClient(HttpClient httpClient, ILogger<ODataClient> logger, IOptions<ODataClientOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        private string GetFullUrl(string path) => $"{_options.ODataUrlSuffix}{path}";

        /// <summary>
        /// Retrieves an entity or a collection of entities from the OData service.
        /// </summary>
        /// <typeparam name="TEntity">The type to deserialize the response content into.</typeparam>
        /// <param name="path">The relative path to the OData resource (e.g., "accounts(GUID)").</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The deserialized entity or collection.</returns>
        public async Task<TEntity> GetAsync<TEntity>(string path, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetFullUrl(path));
            request.Headers.Add("Prefer", "odata.include-annotations=\"*\"");

            return await SendRequestAsync<TEntity>(request, cancellationToken);
        }

        /// <summary>
        /// Retrieves a collection of entities with server-side pagination.
        /// </summary>
        /// <typeparam name="TEntity">The type to deserialize the response content into.</typeparam>
        /// <param name="path">The relative path to the OData resource (e.g., "contacts?$select=...").</param>
        /// <param name="pageSize">The number of records to return per page.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The deserialized collection of entities for the requested page.</returns>
        public async Task<TEntity> GetWithPaginationAsync<TEntity>(string path, int pageSize, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetFullUrl(path));
            request.Headers.Add("Prefer", $"odata.include-annotations=\"*\", odata.maxpagesize={pageSize}");

            return await SendRequestAsync<TEntity>(request, cancellationToken);
        }

        /// <summary>
        /// Updates an existing entity using a PATCH request.
        /// </summary>
        /// <typeparam name="T">The type of the request data object.</typeparam>
        /// <typeparam name="TEntity">The type to deserialize the response into (often not needed for PATCH).</typeparam>
        /// <param name="path">The relative path to the OData resource.</param>
        /// <param name="data">The object containing the properties to update.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The deserialized response content.</returns>
        public async Task<TEntity> PatchAsync<T, TEntity>(string path, T data, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), GetFullUrl(path))
            {
                Content = CreateJsonContent(data)
            };
            return await SendRequestAsync<TEntity>(request, cancellationToken);
        }

        /// <summary>
        /// Creates a new entity using a POST request.
        /// </summary>
        /// <typeparam name="T">The type of the request data object.</typeparam>
        /// <typeparam name="TEntity">The type to deserialize the response into.</typeparam>
        /// <param name="path">The relative path to the entity set (e.g., "accounts").</param>
        /// <param name="data">The object representing the new entity.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The deserialized response content.</returns>
        public async Task<TEntity> PostAsync<T, TEntity>(string path, T data, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetFullUrl(path))
            {
                Content = CreateJsonContent(data)
            };
            return await SendRequestAsync<TEntity>(request, cancellationToken);
        }

        /// <summary>
        /// Updates an existing entity and requests the server to return a representation of the updated entity.
        /// </summary>
        /// <typeparam name="T">The type of the request data object.</typeparam>
        /// <typeparam name="TEntity">The type to deserialize the returned entity into.</typeparam>
        /// <param name="path">The relative path to the OData resource.</param>
        /// <param name="data">The object containing the properties to update.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The deserialized, updated entity.</returns>
        public async Task<TEntity> PatchAndReturnRepresentationAsync<T, TEntity>(string path, T data, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), GetFullUrl(path))
            {
                Content = CreateJsonContent(data)
            };
            request.Headers.Add("Prefer", "return=representation");
            return await SendRequestAsync<TEntity>(request, cancellationToken);
        }

        /// <summary>
        /// Creates a new entity and requests the server to return a representation of the created entity.
        /// </summary>
        /// <typeparam name="T">The type of the request data object.</typeparam>
        /// <typeparam name="TEntity">The type to deserialize the returned entity into.</typeparam>
        /// <param name="path">The relative path to the entity set.</param>
        /// <param name="data">The object representing the new entity.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The deserialized, created entity.</returns>
        public async Task<TEntity> PostAndReturnRepresentationAsync<T, TEntity>(string path, T data, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetFullUrl(path))
            {
                Content = CreateJsonContent(data)
            };
            request.Headers.Add("Prefer", "return=representation");
            return await SendRequestAsync<TEntity>(request, cancellationToken);
        }

        /// <summary>
        /// Deletes an entity from the OData service.
        /// </summary>
        /// <param name="path">The relative path to the OData resource to delete.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, GetFullUrl(path));
            await SendRequestAsync<object>(request, cancellationToken);
        }

        /// <summary>
        /// Associates two entities by creating a relationship.
        /// </summary>
        /// <param name="parentEntityPath">The path to the parent entity (e.g., "accounts(GUID)").</param>
        /// <param name="navigationProperty">The name of the navigation property on the parent entity.</param>
        /// <param name="relatedEntityPath">The full path to the related entity (e.g., "https://.../api/data/v9.2/contacts(GUID)").</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        public async Task AssociateAsync(string parentEntityPath, string navigationProperty, string relatedEntityPath, CancellationToken cancellationToken = default)
        {
            var payload = new JObject { { "@odata.id", relatedEntityPath } };
            var request = new HttpRequestMessage(HttpMethod.Post, GetFullUrl($"{parentEntityPath}/{navigationProperty}/$ref"))
            {
                Content = CreateJsonContent(payload)
            };
            await SendRequestAsync<object>(request, cancellationToken);
        }

        /// <summary>
        /// Disassociates an entity from a single-valued navigation property.
        /// </summary>
        /// <param name="parentEntityPath">The path to the parent entity (e.g., "orders(123)").</param>
        /// <param name="navigationProperty">The name of the single-valued navigation property (e.g., "customer").</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        public async Task DisassociateAsync(string parentEntityPath, string navigationProperty, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, GetFullUrl($"{parentEntityPath}/{navigationProperty}/$ref"));
            await SendRequestAsync<object>(request, cancellationToken);
        }

        /// <summary>
        /// Disassociates an entity from a collection-valued navigation property.
        /// </summary>
        /// <param name="parentEntityPath">The path to the parent entity (e.g., "accounts(GUID)").</param>
        /// <param name="navigationProperty">The name of the collection-valued navigation property (e.g., "contacts").</param>
        /// <param name="relatedEntityKey">The key of the related entity to disassociate (e.g., a GUID string).</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        public async Task DisassociateAsync(string parentEntityPath, string navigationProperty, string relatedEntityKey, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, GetFullUrl($"{parentEntityPath}/{navigationProperty}({relatedEntityKey})/$ref"));
            await SendRequestAsync<object>(request, cancellationToken);
        }

        /// <summary>
        /// Sends a multipart batch request to the OData service.
        /// </summary>
        /// <param name="content">The multipart content representing the batch request.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A list of <see cref="BatchReadResponse"/> for query operations, or an empty list for change sets.</returns>
        public async Task<List<BatchReadResponse>> SendBulkRequestAsync(MultipartContent content, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetFullUrl("$batch"))
            {
                Content = content
            };

            try
            {
                _logger.LogInformation("Sending OData $batch request.");
                using (var response = await _httpClient.SendAsync(request, cancellationToken))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("OData $batch request failed with status code {StatusCode}. Response: {Response}", response.StatusCode, responseContent);
                        throw new ODataClientException($"Batch request failed with status code {response.StatusCode}", response.StatusCode, responseContent);
                    }

                    _logger.LogInformation("OData $batch request succeeded.");
                    return await ParseBulkResponseAsync(response);
                }
            }
            catch (Exception ex) when (ex is not ODataClientException)
            {
                _logger.LogError(ex, "An unexpected error occurred while sending OData $batch request.");
                throw;
            }
        }

        #region Bulk Request Helpers

        /// <summary>
        /// Creates an <see cref="HttpMessageContent"/> for a POST operation inside a batch request.
        /// </summary>
        /// <param name="path">The relative path to the entity set.</param>
        /// <param name="data">The entity data to post.</param>
        /// <param name="contentId">An optional ID to correlate requests and responses within the batch.</param>
        /// <returns>An HttpMessageContent object ready to be added to a MultipartContent collection.</returns>
        public HttpMessageContent PostBulkMessageContent(string path, object data, int? contentId = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_httpClient.BaseAddress, GetFullUrl(path)))
            {
                Content = CreateJsonContent(data)
            };
            return CreateBulkMessageContent(request, contentId);
        }

        /// <summary>
        /// Creates an <see cref="HttpMessageContent"/> for a PATCH operation inside a batch request.
        /// </summary>
        /// <param name="path">The relative path to the entity resource.</param>
        /// <param name="data">The entity data to patch.</param>
        /// <param name="contentId">An optional ID to correlate requests and responses within the batch.</param>
        /// <returns>An HttpMessageContent object ready to be added to a MultipartContent collection.</returns>
        public HttpMessageContent PatchBulkMessageContent(string path, object data, int? contentId = null)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), new Uri(_httpClient.BaseAddress, GetFullUrl(path)))
            {
                Content = CreateJsonContent(data)
            };
            return CreateBulkMessageContent(request, contentId);
        }

        /// <summary>
        /// Creates an <see cref="HttpMessageContent"/> for a DELETE operation inside a batch request.
        /// </summary>
        /// <param name="path">The relative path to the entity resource to delete.</param>
        /// <param name="contentId">An optional ID to correlate requests and responses within the batch.</param>
        /// <returns>An HttpMessageContent object ready to be added to a MultipartContent collection.</returns>
        public HttpMessageContent DeleteBulkMessageContent(string path, int? contentId = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(_httpClient.BaseAddress, GetFullUrl(path)));
            return CreateBulkMessageContent(request, contentId);
        }

        /// <summary>
        /// Creates an <see cref="HttpMessageContent"/> for a GET operation inside a batch request.
        /// </summary>
        /// <param name="path">The relative path to the entity resource to retrieve.</param>
        /// <param name="contentId">An optional ID to correlate requests and responses within the batch.</param>
        /// <returns>An HttpMessageContent object ready to be added to a MultipartContent collection.</returns>
        public HttpMessageContent GetBulkMessageContent(string path, int? contentId = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_httpClient.BaseAddress, GetFullUrl(path)));
            return CreateBulkMessageContent(request, contentId);
        }

        private HttpMessageContent CreateBulkMessageContent(HttpRequestMessage request, int? contentId)
        {
            var messageContent = new HttpMessageContent(request);
            messageContent.Headers.Remove("Content-Type");
            messageContent.Headers.Add("Content-Type", "application/http");
            messageContent.Headers.Add("Content-Transfer-Encoding", "binary");
            if (contentId.HasValue)
            {
                messageContent.Headers.Add("Content-ID", contentId.Value.ToString());
            }
            return messageContent;
        }

        #endregion

        private StringContent CreateJsonContent<T>(T data)
        {
            var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<TEntity> SendRequestAsync<TEntity>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Sending OData request: {Method} {Uri}", request.Method, request.RequestUri);

                using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("OData request failed with status code {StatusCode}. Response: {Response}", response.StatusCode, responseContent);
                        // Throw a custom exception with details
                        throw new ODataClientException($"Request failed with status code {response.StatusCode}", response.StatusCode, responseContent);
                    }

                    _logger.LogInformation("OData request succeeded.");

                    // Handle cases like 204 No Content where there's no body to deserialize
                    if (response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(responseContent))
                    {
                        return default; // Return default value for TEntity (e.g., null for objects)
                    }

                    return JsonConvert.DeserializeObject<TEntity>(responseContent);
                }
            }
            catch (Exception ex) when (ex is not ODataClientException)
            {
                _logger.LogError(ex, "An unexpected error occurred while sending OData request.");
                throw; // Re-throw the original exception
            }
        }

        private async Task<List<BatchReadResponse>> ParseBulkResponseAsync(HttpResponseMessage response)
        {
            var parsedResponses = new List<BatchReadResponse>();
            var multipartResponse = await response.Content.ReadAsMultipartAsync();

            foreach (var contentPart in multipartResponse.Contents)
            {
                if (contentPart.Headers.ContentType?.MediaType != "application/http") continue;

                var httpResponse = await contentPart.ReadAsHttpResponseMessageAsync();
                var jsonString = await httpResponse.Content.ReadAsStringAsync();

                string contentId = null;
                if (contentPart.Headers.TryGetValues("Content-ID", out var values))
                {
                    contentId = values.FirstOrDefault();
                }

                if (httpResponse.IsSuccessStatusCode)
                {
                    if (string.IsNullOrEmpty(jsonString)) continue; // Likely a successful CUD operation with no body

                    var responseData = JObject.Parse(jsonString);
                    var records = responseData["value"] as JArray;
                    var contextUrl = responseData["@odata.context"]?.ToString() ?? "";
                    var entitySetName = ODataHelpers.ParseEntitySetFromContextUrl(contextUrl);

                    if (!string.IsNullOrEmpty(entitySetName) && records != null)
                    {
                        parsedResponses.Add(new BatchReadResponse
                        {
                            ContentId = contentId,
                            IsSuccess = true,
                            EntitySetName = entitySetName,
                            Records = records.OfType<JObject>().ToList()
                        });
                    }
                }
                else
                {
                    // Add a failure response to the list
                    parsedResponses.Add(new BatchReadResponse
                    {
                        ContentId = contentId,
                        IsSuccess = false,
                        ErrorJson = jsonString
                    });
                }
            }
            return parsedResponses;
        }
    }
}
