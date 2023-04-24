using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

public static class HttpClientBuilderExtensions
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
        builder.Services.TryAddSingleton<IClientCredentialsTokenManagementService, ClientCredentialsTokenManagementService>();
        builder.Services.TryAddSingleton<IOpenIdConfigurationRetriever, OpenIdConfigurationRetrieverWithCache>();
        builder.Services.TryAddSingleton<IClientCredentialsTokenSerializer, ClientCredentialsTokenProtectedSerializer>();

        // Using concrete IConfigureOptions<> classes ensure we only add the http message handlers once. The reasons are:
        // - We don't want to authenticate consumer HTTP requests twice or even more
        // - We don't want internal identity provider HTTP requests retry count to grow exponentially
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<HttpClientFactoryOptions>, AddBackchannelRetryHandlerConfigureOptions>());

        var tokenHandlerNamedConfigureOptionsAlreadyAdded = builder.Services.Any(x => IsTokenHandlerConfigureOptionsServiceDescriptor(x, builder.Name));
        if (!tokenHandlerNamedConfigureOptionsAlreadyAdded)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<HttpClientFactoryOptions>>(new AddClientCredentialsTokenHandlerConfigureOptions(builder.Name)));
        }

        return builder;
    }

    private static bool IsTokenHandlerConfigureOptionsServiceDescriptor(ServiceDescriptor serviceDescriptor, string name)
    {
        return serviceDescriptor.ServiceType == typeof(IConfigureOptions<HttpClientFactoryOptions>)
            && serviceDescriptor.ImplementationInstance is AddClientCredentialsTokenHandlerConfigureOptions configureOptions
            && configureOptions.Name == name;
    }

    private sealed class AddBackchannelRetryHandlerConfigureOptions : ConfigureNamedOptions<HttpClientFactoryOptions>
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

    private sealed class AddClientCredentialsTokenHandlerConfigureOptions : ConfigureNamedOptions<HttpClientFactoryOptions>
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
                var tokenHandler = new ClientCredentialsTokenHttpMessageHandler(tokenManagementService, builder.Name);
                builder.AdditionalHandlers.Add(tokenHandler);
            });
        }
    }
}