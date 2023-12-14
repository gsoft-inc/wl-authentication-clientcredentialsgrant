using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

internal sealed class ClientCredentialsPostConfigureOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly string _authScheme;

    public ClientCredentialsPostConfigureOptions(string authScheme)
    {
        this._authScheme = authScheme;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (name == this._authScheme)
        {
            // Default is "AuthenticationTypes.Federation". With this one we can now distinguish identities created by our library
            options.TokenValidationParameters.AuthenticationType = ClientCredentialsDefaults.AuthenticationType;

            // Since .NET 7, there's a breaking change where the default value "true" is changed to "false" if the developer
            // defines a non-empty built-in ASP.NET Core configuration section "Authentication:Schemes:<AuthenticationScheme>".
            // See: https://github.com/dotnet/aspnetcore/blob/v7.0.0/src/Security/Authentication/JwtBearer/src/JwtBearerConfigureOptions.cs#L74-L76
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateIssuer = true;
        }
    }
}