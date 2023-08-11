// This file is based on https://github.com/DuendeSoftware/Duende.AccessTokenManagement/blob/1.1.0/src/Duende.AccessTokenManagement/ClientCredentialsTokenHandler.cs
// Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0. All rights reserved.
//
// The original file has been significantly modified, and these modifications are Copyright (c) Workleap, 2023.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Http;
using Polly;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

/// <summary>
/// HttpMessageHandler injected into the consumer's HttpClient.
/// Adds HTTP Authentication headers with the access token retrieved from the client credentials grant flow.
/// Retries the consumer's HTTP request only once using Polly with a new token if the previous token was expired or revoked.
/// </summary>
internal sealed class ClientCredentialsTokenHttpMessageHandler : PolicyHttpMessageHandler
{
    private const string RetryCountContextKey = "RetryCount";

    // An expired or revoked token could result in an unauthorized HTTP error that we must retry only once with a new token
    private static readonly IAsyncPolicy<HttpResponseMessage> RetryUnauthorizedResponseOnceAsyncPolicy = Policy<HttpResponseMessage>
        .HandleResult(response => response.StatusCode == HttpStatusCode.Unauthorized)
        .RetryAsync((_, retryCount, context) => context[RetryCountContextKey] = retryCount);

    private readonly IClientCredentialsTokenManagementService _tokenManagementService;
    private readonly string _clientName;
    private readonly ClientCredentialsOptions _options;

    public ClientCredentialsTokenHttpMessageHandler(IClientCredentialsTokenManagementService tokenManagementService, string clientName, ClientCredentialsOptions options)
        : base(RetryUnauthorizedResponseOnceAsyncPolicy)
    {
        this._tokenManagementService = tokenManagementService;
        this._clientName = clientName;
        this._options = options;
    }

    protected override async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, Context context, CancellationToken cancellationToken)
    {
        this.EnsureRequestIsSentOverHttps(request);

        // Retry with a new token if the previous attempt was unauthorized
        var cachingBehavior = context.ContainsKey(RetryCountContextKey) ? CachingBehavior.ForceRefresh : CachingBehavior.PreferCache;
        await this.SetTokenAsync(request, cachingBehavior, cancellationToken).ConfigureAwait(false);
        return await base.SendCoreAsync(request, context, cancellationToken).ConfigureAwait(false);
    }

    private void EnsureRequestIsSentOverHttps(HttpRequestMessage request)
    {
        if (this._options.EnforceHttps && request.RequestUri is { IsAbsoluteUri: true } requestUri && requestUri.Scheme != "https")
        {
            throw new ClientCredentialsException("Due to security concerns, authenticated requests must be sent over HTTPS");
        }
    }

    private async Task SetTokenAsync(HttpRequestMessage request, CachingBehavior cachingBehavior, CancellationToken cancellationToken)
    {
        var token = await this._tokenManagementService.GetAccessTokenAsync(this._clientName, cachingBehavior, cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }
}