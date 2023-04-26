// This file is based on https://github.com/DuendeSoftware/Duende.AccessTokenManagement/blob/1.1.0/src/Duende.AccessTokenManagement/ClientCredentialsTokenManagementService.cs
// Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0. All rights reserved.
//
// The original file has been significantly modified, and these modifications are Copyright (c) GSoft Group Inc., 2023.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

/// <summary>
/// Service responsible for retrieving a client credentials grant-based access token from the cache or an OAuth 2.0 identity provider.
/// Also prevents unnecessary tokens creation using Lazy&lt;Task&gt;.
/// </summary>
internal class ClientCredentialsTokenManagementService : IClientCredentialsTokenManagementService
{
    private readonly ConcurrentDictionary<string, Lazy<Task<ClientCredentialsToken>>> _lazyGetNewTokenTasks;
    private readonly IClientCredentialsTokenEndpointService _tokenEndpointService;
    private readonly IClientCredentialsTokenCache _tokenCache;

    public ClientCredentialsTokenManagementService(IClientCredentialsTokenEndpointService tokenEndpointService, IClientCredentialsTokenCache tokenCache)
    {
        // .NET named options are case sensitive (https://learn.microsoft.com/en-us/dotnet/core/extensions/options#named-options-support-using-iconfigurenamedoptions)
        this._lazyGetNewTokenTasks = new ConcurrentDictionary<string, Lazy<Task<ClientCredentialsToken>>>(StringComparer.Ordinal);

        this._tokenEndpointService = tokenEndpointService;
        this._tokenCache = tokenCache;
    }

    public async Task<ClientCredentialsToken> GetAccessTokenAsync(string clientName, CachingBehavior cachingBehavior, CancellationToken cancellationToken)
    {
        if (cachingBehavior == CachingBehavior.PreferCache)
        {
            var cachedToken = await this._tokenCache.GetAsync(clientName, cancellationToken).ConfigureAwait(false);
            if (cachedToken != null)
            {
                return cachedToken;
            }
        }

        async Task<ClientCredentialsToken> GetNewAccessTokenAsync()
        {
            var newToken = await this._tokenEndpointService.RequestTokenAsync(clientName, cancellationToken).ConfigureAwait(false);
            await this._tokenCache.SetAsync(clientName, newToken, cancellationToken).ConfigureAwait(false);
            return newToken;
        }

        return await this.SynchronizeAsync(clientName, GetNewAccessTokenAsync).ConfigureAwait(false);
    }

    private async Task<ClientCredentialsToken> SynchronizeAsync(string clientName, Func<Task<ClientCredentialsToken>> getTokenTaskFactory)
    {
        try
        {
            return await this._lazyGetNewTokenTasks.GetOrAdd(clientName, _ => new Lazy<Task<ClientCredentialsToken>>(getTokenTaskFactory)).Value.ConfigureAwait(false);
        }
        finally
        {
            this._lazyGetNewTokenTasks.TryRemove(clientName, out _);
        }
    }
}