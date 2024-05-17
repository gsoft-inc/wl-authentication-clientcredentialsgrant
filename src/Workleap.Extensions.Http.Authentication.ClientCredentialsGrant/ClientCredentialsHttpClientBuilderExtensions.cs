using Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ClientCredentialsHttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddClientCredentialsHandler(this IHttpClientBuilder builder, Action<ClientCredentialsOptions>? configure = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure != null)
        {
            builder.Services.Configure(builder.Name, configure);
        }

        builder.Services.AddOptions();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<ClientCredentialsOptions>, PostConfigureClientCredentialsOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ClientCredentialsOptions>, ValidateClientCredentialsOptions>());

        builder.Services.AddDistributedMemoryCache();

        builder.Services.TryAddSingleton<IClientCredentialsTokenCache, ClientCredentialsTokenCache>();
        builder.Services.TryAddSingleton<IClientCredentialsTokenEndpointService, ClientCredentialsTokenEndpointService>();
        builder.Services.TryAddSingleton<ClientCredentialsTokenManagementService>();
        builder.Services.TryAddSingleton<IClientCredentialsTokenManagementService>(x => x.GetRequiredService<ClientCredentialsTokenManagementService>());
        builder.Services.TryAddSingleton<IOpenIdConfigurationRetriever, OpenIdConfigurationRetrieverWithCache>();
        builder.Services.TryAddSingleton<IClientCredentialsTokenSerializer, ClientCredentialsTokenSerializer>();

        // Using concrete IConfigureOptions<> classes ensure we only add the http message handlers once. The reasons are:
        // - We don't want to authenticate consumer HTTP requests twice or even more
        // - We don't want internal identity provider HTTP requests retry count to grow exponentially
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<HttpClientFactoryOptions>, AddBackchannelRetryHandlerConfigureOptions>());

        var tokenHandlerNamedConfigureOptionsAlreadyAdded = builder.Services.Any(x => IsTokenHandlerConfigureOptionsServiceDescriptor(x, builder.Name));
        if (!tokenHandlerNamedConfigureOptionsAlreadyAdded)
        {
            builder.Services.Add(ServiceDescriptor.Singleton<IConfigureOptions<HttpClientFactoryOptions>>(new AddClientCredentialsTokenHandlerConfigureOptions(builder.Name)));
        }

        builder.Services.Configure<CacheTokenOnStartupBackgroundServiceOptions>(options =>
        {
            options.ClientCredentialPoweredClientNames.Add(builder.Name);
        });

        // This background service is directly accessed in integration tests to ensure the token is cached on startup
        builder.Services.TryAddSingleton<CacheTokenOnStartupBackgroundService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CacheTokenOnStartupBackgroundService>(x => x.GetRequiredService<CacheTokenOnStartupBackgroundService>()));

        return builder;
    }

    private static bool IsTokenHandlerConfigureOptionsServiceDescriptor(ServiceDescriptor serviceDescriptor, string name)
    {
        return serviceDescriptor.ServiceType == typeof(IConfigureOptions<HttpClientFactoryOptions>)
            && serviceDescriptor.ImplementationInstance is AddClientCredentialsTokenHandlerConfigureOptions configureOptions
            && configureOptions.Name == name;
    }

    internal sealed class AddBackchannelRetryHandlerConfigureOptions : ConfigureNamedOptions<HttpClientFactoryOptions>
    {
        public AddBackchannelRetryHandlerConfigureOptions()
            : base(ClientCredentialsConstants.BackchannelHttpClientName, AddBackchannelRetryHandler)
        {
        }

        private static void AddBackchannelRetryHandler(HttpClientFactoryOptions options)
        {
            options.HttpMessageHandlerBuilderActions.Add(static builder =>
            {
                builder.AdditionalHandlers.Add(new BackchannelRetryHttpMessageHandler());
            });
        }
    }

    internal sealed class AddClientCredentialsTokenHandlerConfigureOptions : ConfigureNamedOptions<HttpClientFactoryOptions>
    {
        public AddClientCredentialsTokenHandlerConfigureOptions(string name)
            : base(name, AddClientCredentialsTokenHandler)
        {
        }

        private static void AddClientCredentialsTokenHandler(HttpClientFactoryOptions options)
        {
            options.HttpMessageHandlerBuilderActions.Add(static builder =>
            {
                var tokenManagementService = builder.Services.GetRequiredService<IClientCredentialsTokenManagementService>();
                var options = builder.Services.GetRequiredService<IOptionsMonitor<ClientCredentialsOptions>>().Get(builder.Name);

                var tokenHandler = new ClientCredentialsTokenHttpMessageHandler(tokenManagementService, builder.Name, options);
                builder.AdditionalHandlers.Add(tokenHandler);
            });
        }
    }
}