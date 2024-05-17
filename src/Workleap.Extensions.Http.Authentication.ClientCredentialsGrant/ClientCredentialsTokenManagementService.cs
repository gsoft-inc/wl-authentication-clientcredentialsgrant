// This file is based on https://github.com/DuendeSoftware/Duende.AccessTokenManagement/blob/1.1.0/src/Duende.AccessTokenManagement/ClientCredentialsTokenManagementService.cs
// Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0. All rights reserved.
//
// The original file has been significantly modified, and these modifications are Copyright (c) Workleap, 2023.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

/// <summary>
/// Service responsible for retrieving a client credentials grant-based access token from the cache or an OAuth 2.0 identity provider.
/// Also prevents unnecessary tokens creation using Lazy&lt;Task&gt;.
/// </summary>
internal class ClientCredentialsTokenManagementService : IClientCredentialsTokenManagementService
{
    private readonly ConcurrentDictionary<string, Lazy<Task<ClientCredentialsToken>>> _lazyGetNewTokenTasks;
    private readonly IClientCredentialsTokenEndpointService _tokenEndpointService;
    private readonly IClientCredentialsTokenCache _tokenCache;
    private readonly CancellationToken _backgroundRefreshCancellationToken;

    public ClientCredentialsTokenManagementService(
        IClientCredentialsTokenEndpointService tokenEndpointService,
        IClientCredentialsTokenCache tokenCache,
        IHostApplicationLifetime? applicationLifetime = null)
    {
        // .NET named options are case sensitive (https://learn.microsoft.com/en-us/dotnet/core/extensions/options#named-options-support-using-iconfigurenamedoptions)
        this._lazyGetNewTokenTasks = new ConcurrentDictionary<string, Lazy<Task<ClientCredentialsToken>>>(StringComparer.Ordinal);

        this._tokenEndpointService = tokenEndpointService;
        this._tokenCache = tokenCache;

        // If the application is not using a .NET generic host, the application lifetime will be null
        this._backgroundRefreshCancellationToken = applicationLifetime?.ApplicationStopping ?? CancellationToken.None;
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

        return await this.SynchronizeAsync(clientName, this.GetNewTokenTaskFactory(clientName, cancellationToken)).ConfigureAwait(false);
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

    private Func<Task<ClientCredentialsToken>> GetNewTokenTaskFactory(string clientName, CancellationToken cancellationToken) => async () =>
    {
        var newToken = await this._tokenEndpointService.RequestTokenAsync(clientName, cancellationToken).ConfigureAwait(false);

        var cacheEvictionTime = await this._tokenCache.SetAsync(clientName, newToken, cancellationToken).ConfigureAwait(false);
        var cacheDuration = cacheEvictionTime - DateTimeOffset.UtcNow;

        if (cacheDuration > TimeSpan.Zero)
        {
            const double backgroundRefreshDelayFactor = 0.8;
            var delayBeforeNextBackgroundRefresh = TimeSpan.FromTicks((long)Math.Round(cacheDuration.Ticks * backgroundRefreshDelayFactor));

            _ = this.ScheduleTokenBackgroundRefreshAsync(clientName, delayBeforeNextBackgroundRefresh);
        }

        return newToken;
    };

    private async Task ScheduleTokenBackgroundRefreshAsync(string clientName, TimeSpan delayBeforeNextBackgroundRefresh)
    {
        await Task.Delay(delayBeforeNextBackgroundRefresh, this._backgroundRefreshCancellationToken).ConfigureAwait(false);

        try
        {
            // We don't care about the token, we just want to refresh it in the background and have it cached
            _ = await this.GetAccessTokenAsync(clientName, CachingBehavior.ForceRefresh, this._backgroundRefreshCancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (this._backgroundRefreshCancellationToken.IsCancellationRequested)
        {
            // Expected when the application is shutting down
        }
    }
}