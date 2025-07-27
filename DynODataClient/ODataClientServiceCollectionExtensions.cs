// =================================================================================
// File: ODataClientServiceCollectionExtensions.cs
// Description: Provides easy-to-use extension methods for IServiceCollection
//              to register the ODataClient and its authentication handlers.
//              This is the main entry point for consumers of the library.
// =---------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DynODataClient
{
    /// <summary>
    /// Extension methods for setting up the ODataClient in an IServiceCollection.
    /// </summary>
    public static class ODataClientServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the ODataClient to the services collection, configured to use Dynamics 365 (OAuth) authentication.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="optionsSectionName">The name of the configuration section containing DynamicsAuthOptions.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddODataClientWithDynamicsAuth(this IServiceCollection services, IConfiguration configuration, string optionsSectionName = "ODataOptions")
        {
            if (string.IsNullOrEmpty(optionsSectionName))
            {
                throw new ArgumentNullException(nameof(optionsSectionName));
            }

            // Configure the options from the app's configuration file
            services.Configure<DynamicAuthOptions>(configuration.GetSection(optionsSectionName));
            // Make the generic options available as well
            services.AddSingleton<IOptions<ODataClientOptions>>(x => x.GetRequiredService<IOptions<DynamicAuthOptions>>());


            // Register the authentication handler
            services.AddTransient<DynamicAuthHandler>();

            // Register the ODataClient with a typed HttpClient
            services.AddHttpClient<ODataClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<DynamicAuthOptions>>().Value;
                if (string.IsNullOrEmpty(options.BaseUrl))
                {
                    throw new InvalidOperationException("ODataOptions:BaseUrl is not configured.");
                }

                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            })
            .AddHttpMessageHandler<DynamicAuthHandler>();

            return services;
        }

        /// <summary>
        /// Adds the ODataClient to the services collection, configured to use Basic Authentication.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="optionsSectionName">The name of the configuration section containing BasicAuthOptions.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddODataClientWithBasicAuth(this IServiceCollection services, IConfiguration configuration, string optionsSectionName = "ODataOptions")
        {
            if (string.IsNullOrEmpty(optionsSectionName))
            {
                throw new ArgumentNullException(nameof(optionsSectionName));
            }

            // Configure the options from the app's configuration file
            services.Configure<BasicAuthOptions>(configuration.GetSection(optionsSectionName));
            // Make the generic options available as well
            services.AddSingleton<IOptions<ODataClientOptions>>(x => x.GetRequiredService<IOptions<BasicAuthOptions>>());

            // Register the authentication handler
            services.AddTransient<BasicAuthDelegatingHandler>();

            // Register the ODataClient with a typed HttpClient
            services.AddHttpClient<ODataClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<BasicAuthOptions>>().Value;
                if (string.IsNullOrEmpty(options.BaseUrl))
                {
                    throw new InvalidOperationException("ODataOptions:BaseUrl is not configured.");
                }

                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            })
            .AddHttpMessageHandler<BasicAuthDelegatingHandler>();

            return services;
        }
    }
}
