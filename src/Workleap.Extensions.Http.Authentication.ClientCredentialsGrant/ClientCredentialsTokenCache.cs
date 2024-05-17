// This file is based on https://github.com/DuendeSoftware/Duende.AccessTokenManagement/blob/1.1.0/src/Duende.AccessTokenManagement/DistributedClientCredentialsTokenCache.cs
// Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0. All rights reserved.
//
// The original file has been significantly modified, and these modifications are Copyright (c) Workleap, 2023.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

/// <summary>
/// An implementation of a token cache made of two cache levels.
/// We use a memory cache (L1) for faster look up, then a distributed cache (L2).
/// </summary>
internal sealed class ClientCredentialsTokenCache : IClientCredentialsTokenCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IClientCredentialsTokenSerializer _tokenSerializer;
    private readonly IOptionsMonitor<ClientCredentialsOptions> _optionsMonitor;

    public ClientCredentialsTokenCache(IDistributedCache distributedCache, IClientCredentialsTokenSerializer tokenSerializer, IOptionsMonitor<ClientCredentialsOptions> optionsMonitor)
        : this(new MemoryCache(new MemoryCacheOptions()), distributedCache, tokenSerializer, optionsMonitor)
    {
    }

    // Constructor used by tests
    internal ClientCredentialsTokenCache(IMemoryCache memoryCache, IDistributedCache distributedCache, IClientCredentialsTokenSerializer tokenSerializer, IOptionsMonitor<ClientCredentialsOptions> optionsMonitor)
    {
        this._memoryCache = memoryCache;
        this._distributedCache = distributedCache;
        this._tokenSerializer = tokenSerializer;
        this._optionsMonitor = optionsMonitor;
    }

    public async Task<DateTimeOffset> SetAsync(string clientName, ClientCredentialsToken token, CancellationToken cancellationToken)
    {
        var options = this._optionsMonitor.Get(clientName);

        var tokenBytes = this._tokenSerializer.Serialize(clientName, token);
        var cacheEvictionTime = token.Expiration.Subtract(options.CacheLifetimeBuffer);

        // Store token in L1 first
        this._memoryCache.Set(options.CacheKey, tokenBytes, new MemoryCacheEntryOptions { AbsoluteExpiration = cacheEvictionTime });

        // Then store token in L2
        var distributedCacheOptions = new DistributedCacheEntryOptions { AbsoluteExpiration = cacheEvictionTime };
        await this._distributedCache.SetAsync(options.CacheKey, tokenBytes, distributedCacheOptions, cancellationToken).ConfigureAwait(false);

        return cacheEvictionTime;
    }

    public async Task<ClientCredentialsToken?> GetAsync(string clientName, CancellationToken cancellationToken)
    {
        var options = this._optionsMonitor.Get(clientName);

        // Read from L1 first
        var l1TokenBytes = this._memoryCache.Get<byte[]?>(options.CacheKey);
        if (l1TokenBytes != null)
        {
            return this._tokenSerializer.Deserialize(clientName, l1TokenBytes);
        }

        // Then read from L2 if not found in L1
        var l2TokenBytes = await this._distributedCache.GetAsync(options.CacheKey, cancellationToken).ConfigureAwait(false);
        if (l2TokenBytes == null)
        {
            return null;
        }

        var token = this._tokenSerializer.Deserialize(clientName, l2TokenBytes);

        // Promote L2-cached token to L1
        var absoluteExpiration = token.Expiration.Subtract(options.CacheLifetimeBuffer);
        this._memoryCache.Set(options.CacheKey, l2TokenBytes, new MemoryCacheEntryOptions { AbsoluteExpiration = absoluteExpiration });

        return token;
    }
}