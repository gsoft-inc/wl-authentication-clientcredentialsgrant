using System.Net;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

/// <summary>
/// Retry handler for HTTP requests made to OAuth 2.0 identity providers,
/// for instance for retrieving tokens or the metadata (.well-known/openid-configuration).
/// </summary>
internal sealed class BackchannelRetryHttpMessageHandler : PolicyHttpMessageHandler
{
    private const HttpStatusCode TooManyRequestsStatusCode = (HttpStatusCode)429;

    public BackchannelRetryHttpMessageHandler()
        : base(GetRetryPolicy())
    {
    }

    // Microsoft documentation about resilient HTTP requests and production-grade retry jitter strategy:
    // https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly#add-a-jitter-strategy-to-the-retry-policy
    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 3);

        return Policy<HttpResponseMessage>
            .HandleResult(response => response.StatusCode == TooManyRequestsStatusCode)
            .OrTransientHttpStatusCode()
            .WaitAndRetryAsync(delay);
    }
}