using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace GSoft.AspNetCore.Authentication.ClientCredentialsGrant;

internal class ValidateJwtBearerOptions : IValidateOptions<JwtBearerOptions>
{
    internal readonly string AuthScheme;

    public ValidateJwtBearerOptions(string authScheme)
    {
        this.AuthScheme = authScheme;
    }

    public ValidateOptionsResult Validate(string name, JwtBearerOptions options)
    {
        var errors = new List<string>();

        if (this.AuthScheme == name)
        {
            // Authority must be an absolute URL starting with HTTPS
            // See: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/4.53.0/src/client/Microsoft.Identity.Client/AppConfig/AuthorityInfo.cs#L362
            if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out var authority))
            {
                errors.Add($"{nameof(options.Authority)} '{options.Authority}' should be in URI format");
            }
            else if (authority.Scheme != "https")
            {
                errors.Add($"{nameof(options.Authority)} '{options.Authority}' should use the 'https' scheme");
            }

            // Client ID cannot be empty, and is not necessarily a GUID (https://www.oauth.com/oauth2-servers/client-registration/client-id-secret/)
            if (string.IsNullOrWhiteSpace(options.Audience))
            {
                errors.Add($"{nameof(options.Audience)} cannot be empty");
            }
        }

        // ValidateOptionsResult(enumerable) will join the errors in a single line, separated by "; "
        // See: https://github.com/dotnet/runtime/blob/v6.0.0/src/libraries/Microsoft.Extensions.Options/src/ValidateOptionsResult.cs#L63
        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}