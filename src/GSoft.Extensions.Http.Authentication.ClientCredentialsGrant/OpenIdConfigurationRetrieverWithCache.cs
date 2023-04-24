using System.Collections.Concurrent;
using System.Text.Json;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

/// <summary>
/// Class responsible for retrieving the OpenID metadata configuration from an identity provider.
/// Inspired from Microsoft.Identity.Client's new generic authority feature:
/// https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/4.53.0/src/client/Microsoft.Identity.Client/Instance/Oidc/OidcRetrieverWithCache.cs
/// </summary>
internal sealed class OpenIdConfigurationRetrieverWithCache : IOpenIdConfigurationRetriever, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, OpenIdConfiguration> _cache;
    private readonly SemaphoreSlim _mutex;

    public OpenIdConfigurationRetrieverWithCache(IHttpClientFactory httpClientFactory)
    {
        this._httpClientFactory = httpClientFactory;
        this._cache = new ConcurrentDictionary<string, OpenIdConfiguration>(StringComparer.OrdinalIgnoreCase);
        this._mutex = new SemaphoreSlim(1);
    }

    public async Task<OpenIdConfiguration> GetAsync(string authority, CancellationToken cancellationToken)
    {
        if (this._cache.TryGetValue(authority, out var configuration))
        {
            return configuration;
        }

        await this._mutex.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (this._cache.TryGetValue(authority, out configuration))
            {
                return configuration;
            }

            var httpClient = this._httpClientFactory.CreateClient(ClientCredentialsConstants.BackchannelHttpClientName);
            var oidcMetadataEndpoint = authority.TrimEnd('/') + "/.well-known/openid-configuration";

            using var responseStream = await httpClient.GetStreamAsync(oidcMetadataEndpoint).ConfigureAwait(false);
            configuration = await JsonSerializer.DeserializeAsync<OpenIdConfiguration>(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false)
                ?? throw new ClientCredentialsException($"Could not retrieve OIDC metadata for authority '{authority}'");

            this._cache[authority] = configuration;
            return configuration;
        }
        finally
        {
            this._mutex.Release();
        }
    }

    public void Dispose()
    {
        this._mutex.Dispose();
    }
}