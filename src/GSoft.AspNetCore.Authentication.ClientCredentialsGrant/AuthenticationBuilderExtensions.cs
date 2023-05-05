using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace GSoft.AspNetCore.Authentication.ClientCredentialsGrant;

public static class AuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddClientCredentialsAuthentication(this AuthenticationBuilder authBuilder)
        => authBuilder.AddClientCredentialsAuthentication(ClientCredentialsDefaults.AuthenticationScheme);

    public static AuthenticationBuilder AddClientCredentialsAuthentication(this AuthenticationBuilder authBuilder, string authScheme)
        => authBuilder.AddClientCredentialsAuthentication(authScheme, _ => { });

    public static AuthenticationBuilder AddClientCredentialsAuthentication(this AuthenticationBuilder authBuilder, string authScheme, Action<JwtBearerOptions> configureOptions)
    {
        if (authBuilder == null)
        {
            throw new ArgumentNullException(nameof(authBuilder));
        }

        authBuilder.AddJwtBearer(authScheme, configureOptions);

        return authBuilder;
    }
}
