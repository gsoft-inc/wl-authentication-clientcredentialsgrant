using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class AuthenticationBuilderExtensions
{
    private const string AuthenticationSchemesKey = "Schemes";
    private const string AuthenticationConfigKey = "Authentication";

    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder builder)
        => builder.AddClientCredentials(ClientCredentialsDefaults.AuthenticationScheme, _ => { });

    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder builder, Action<JwtBearerOptions> configureOptions)
        => builder.AddClientCredentials(ClientCredentialsDefaults.AuthenticationScheme, configureOptions);

    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder builder, string authScheme, Action<JwtBearerOptions> configureOptions)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddOptions<JwtBearerOptions>(authScheme).Configure<IConfiguration>((options, configuration) =>
        {
            var configSection = configuration.GetSection($"{AuthenticationConfigKey}:{AuthenticationSchemesKey}:{authScheme}");
            if (configSection is null || !configSection.GetChildren().Any())
            {
                return;
            }

            configSection.Bind(options);

            // The default interval is 5 minutes which seemed to cause a lot of traffic to the authority server.
            // Users still have the possibility to overwrite this value in their services.
            options.RefreshInterval = TimeSpan.FromHours(12);
        });

        builder.AddJwtBearer(ClientCredentialsDefaults.AuthenticationScheme, configureOptions);

        var tokenHandlerNamedConfigureOptionsAlreadyAdded = builder.Services.Any(x => IsDescriptorOfJwtBearerOptionsValidator(x, authScheme));
        if (!tokenHandlerNamedConfigureOptionsAlreadyAdded)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<JwtBearerOptions>>(new JwtBearerOptionsValidator(authScheme)));
        }

        return builder;
    }

    private static bool IsDescriptorOfJwtBearerOptionsValidator(ServiceDescriptor serviceDescriptor, string name)
    {
        return serviceDescriptor.ServiceType == typeof(IValidateOptions<JwtBearerOptions>)
               && serviceDescriptor.ImplementationInstance is JwtBearerOptionsValidator configureOptions
               && configureOptions.AuthScheme == name;
    }
}