using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;

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

        builder.Services.AddOptions<JwtBearerOptions>(authScheme)
            .Configure<IConfiguration>((options, configuration) =>
        {
            var configSection = configuration.GetSection($"{AuthenticationConfigKey}:{AuthenticationSchemesKey}:{authScheme}");
            if (configSection is null || !configSection.GetChildren().Any())
            {
                return;
            }

            configSection.Bind(options);
        });

        builder.AddJwtBearer(ClientCredentialsDefaults.AuthenticationScheme, configureOptions);
        
        return builder;
    }
}