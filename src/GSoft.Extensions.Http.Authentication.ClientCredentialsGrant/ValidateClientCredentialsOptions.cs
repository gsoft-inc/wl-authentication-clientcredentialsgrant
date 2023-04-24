using Microsoft.Extensions.Options;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class ValidateClientCredentialsOptions : IValidateOptions<ClientCredentialsOptions>
{
    public ValidateOptionsResult Validate(string name, ClientCredentialsOptions options)
    {
        var errors = new List<string>();

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
        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            errors.Add($"{nameof(options.ClientId)} cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            errors.Add($"{nameof(options.ClientSecret)} cannot be empty");
        }

        if (options.Scopes == null)
        {
            errors.Add($"{nameof(options.Scopes)} cannot be null");
        }

        if (options.CacheLifetimeBuffer < TimeSpan.Zero)
        {
            errors.Add($"{nameof(options.CacheLifetimeBuffer)} must be greater than or equal to zero");
        }

        // ValidateOptionsResult(enumerable) will join the errors in a single line, separated by "; "
        // See: https://github.com/dotnet/runtime/blob/v6.0.0/src/libraries/Microsoft.Extensions.Options/src/ValidateOptionsResult.cs#L63
        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}