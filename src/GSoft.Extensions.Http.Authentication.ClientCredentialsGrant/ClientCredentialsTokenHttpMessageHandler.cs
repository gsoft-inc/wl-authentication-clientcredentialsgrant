using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Http;
using Polly;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

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

    public ClientCredentialsTokenHttpMessageHandler(IClientCredentialsTokenManagementService tokenManagementService, string clientName)
        : base(RetryUnauthorizedResponseOnceAsyncPolicy)
    {
        this._tokenManagementService = tokenManagementService;
        this._clientName = clientName;
    }

    protected override async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, Context context, CancellationToken cancellationToken)
    {
        // Retry with a new token if the previous attempt was unauthorized
        var cachingBehavior = context.ContainsKey(RetryCountContextKey) ? CachingBehavior.ForceRefresh : CachingBehavior.PreferCache;
        await this.SetTokenAsync(request, cachingBehavior, cancellationToken).ConfigureAwait(false);
        return await base.SendCoreAsync(request, context, cancellationToken).ConfigureAwait(false);
    }

    private async Task SetTokenAsync(HttpRequestMessage request, CachingBehavior cachingBehavior, CancellationToken cancellationToken)
    {
        var token = await this._tokenManagementService.GetAccessTokenAsync(this._clientName, cachingBehavior, cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }
}