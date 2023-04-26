using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class PostConfigureClientCredentialsOptions : IPostConfigureOptions<ClientCredentialsOptions>
{
    public void PostConfigure(string name, ClientCredentialsOptions options)
    {
        if (string.IsNullOrEmpty(options.CacheKey))
        {
            options.CacheKey = ComputeCacheKey(options);
        }
    }

    private static string ComputeCacheKey(ClientCredentialsOptions options)
    {
        string hashedScopeHex;

        using (var sha = SHA256.Create())
        {
            var hashedScopeBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(options.Scope));
            hashedScopeHex = BitConverter.ToString(hashedScopeBytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        // Appending the hashed scope to the cache key may result in shorter cache keys compared to appending the raw scope
        return $"GSoft.Authentication.ClientCredentialsGrant.{options.ClientId}.{hashedScopeHex}";
    }
}