using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSoft.AspNetCore.Authentication.ClientCredentialsGrant;

public static class AuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder authBuilder, string authScheme)
        => authBuilder.AddClientCredentials(authScheme, _ => { });
    
    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder authBuilder)
    {
        if (authBuilder == null)
        {
            throw new ArgumentNullException(nameof(authBuilder));
        }

        authBuilder.Services.AddOptions<JwtBearerOptions>(ClientCredentialsDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
        {
            var configSection = configuration.GetSection(ClientCredentialsDefaults.ClientCredentialsConfigSection);
            if (configSection is null || !configSection.GetChildren().Any())
            {
                return;
            }

            configSection.Bind(options);
        });

        authBuilder.AddJwtBearer(ClientCredentialsDefaults.AuthenticationScheme, _ => { });
        
        return authBuilder;
    }

    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder authBuilder, string authScheme, Action<JwtBearerOptions> configureOptions)
    {
        if (authBuilder == null)
        {
            throw new ArgumentNullException(nameof(authBuilder));
        }

        authBuilder.AddJwtBearer(authScheme, configureOptions);

        return authBuilder;
    }
}