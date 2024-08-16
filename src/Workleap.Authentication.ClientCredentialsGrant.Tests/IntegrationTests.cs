using System.Diagnostics;
using System.Net;
using Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests;

public sealed partial class IntegrationTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    // Reused in-memory cache across the multipe web app instances to simulate a distributed system
    private readonly MemoryDistributedCache _sharedDistributedCache = new(Options.Create(new MemoryDistributedCacheOptions()));

    // Prevents the test from hanging forever in case of misconfiguration
    private readonly CancellationTokenSource _timeout = Debugger.IsAttached
        ? new CancellationTokenSource()
        : new CancellationTokenSource(TimeSpan.FromMinutes(1));

    private WebApplication _idp = null!;
    private WebApplication _api1 = null!;
    private WebApplication _api2 = null!;

    private int _tokenRequestCount;

    public async Task InitializeAsync()
    {
        // Setup IdentityServer as the identity provider
        this._idp = this.CreateTestIdentityProvider();
        await this._idp.StartAsync(this._timeout.Token);

        // Creating multiple test APIs helps simulate a distributed system with multiple replicas
        this._api1 = this.CreateTestApi("app1", this._idp);
        this._api2 = this.CreateTestApi("app2", this._idp);

        // Ensure that registered clients get cached tokens on app startup
        await this._api1.StartAsync(this._timeout.Token);
        await this._api1.Services.GetRequiredService<OnStartupTokenCacheBackgroundService>().WaitForTokenCachingToCompleteAsync(this._timeout.Token);

        await this._api2.StartAsync(this._timeout.Token);
        await this._api2.Services.GetRequiredService<OnStartupTokenCacheBackgroundService>().WaitForTokenCachingToCompleteAsync(this._timeout.Token);

        // TODO improve background token acquisition so that 2nd app uses the token cached by the 1st app from the distributed cache
        Assert.Equal(2, this._tokenRequestCount);
    }

    [Fact]
    public async Task GivenAnonymousEndpoint_WhenReadAuthenticatedHttpRequest_ShouldBeSuccessful()
    {
        var httpClient = this._api1.Services.GetRequiredService<IHttpClientFactory>().CreateClient(InvoiceReadHttpClientName);
        var response = await httpClient.GetStringAsync("https://invoice-app.local/anonymous", this._timeout.Token);
        Assert.Equal("This endpoint is public", response);
    }

    [Fact]
    public async Task GivenReadEndpointWithClassicPolicy_WhenReadAuthenticatedHttpRequest_ShouldBeSuccessful()
    {
        var httpClient = this._api1.Services.GetRequiredService<IHttpClientFactory>().CreateClient(InvoiceReadHttpClientName);
        var response = await httpClient.GetStringAsync("https://invoice-app.local/read-invoices", this._timeout.Token);
        Assert.Equal("This protected endpoint is for reading invoices", response);
    }

    [Fact]
    public async Task GivenReadEndpointWithGranularPolicy_WhenReadAuthenticatedHttpRequest_ShouldBeSuccessful()
    {
        var httpClient = this._api1.Services.GetRequiredService<IHttpClientFactory>().CreateClient(InvoiceReadHttpClientName);
        var response = await httpClient.GetStringAsync("https://invoice-app.local/read-invoices-granular", this._timeout.Token);
        Assert.Equal("This protected endpoint is for reading invoices", response);
    }

    [Fact]
    public async Task GivenPayEndpointWithClassicPolicy_WhenReadAuthenticatedHttpRequest_ShouldThrowForbiddenHttpException()
    {
        var httpClient = this._api1.Services.GetRequiredService<IHttpClientFactory>().CreateClient(InvoiceReadHttpClientName);
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => httpClient.GetStringAsync("https://invoice-app.local/pay-invoices", this._timeout.Token));
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task GivenPayEndpointWithGranularPolicy_WhenReadAuthenticatedHttpRequest_ShouldThrowForbiddenHttpException()
    {
        var httpClient = this._api1.Services.GetRequiredService<IHttpClientFactory>().CreateClient(InvoiceReadHttpClientName);
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => httpClient.GetStringAsync("https://invoice-app.local/pay-invoices-granular", this._timeout.Token));
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task GivenHttpEndpoint_WhenHttpsIsRequired_ThrowsClientCredentialsException()
    {
        var httpClient = this._api1.Services.GetRequiredService<IHttpClientFactory>().CreateClient(InvoiceReadHttpClientName);
        var ex = await Assert.ThrowsAsync<ClientCredentialsException>(() => httpClient.GetStringAsync("http://invoice-app.local/public", this._timeout.Token));
        Assert.Equal("Due to security concerns, authenticated requests must be sent over HTTPS", ex.Message);
    }

    [Fact]
    public async Task TokenCachingVerification()
    {
        // Retrieve the access token from the cache for later comparison
        // Assert that the token lifetime is set according to the IdentityServer configuration
        var tokenCache = this._api1.Services.GetRequiredService<IClientCredentialsTokenCache>();
        var tokenAfterStartupBackgroundCaching = await tokenCache.GetAsync(InvoiceReadHttpClientName, this._timeout.Token);
        Assert.NotNull(tokenAfterStartupBackgroundCaching);
        Assert.InRange(tokenAfterStartupBackgroundCaching.Expiration, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.Add(TokenLifetime));

        // Make an authenticated HTTP request to the protected endpoint
        var api1HttpClient = this._api1.Services.GetRequiredService<IHttpClientFactory>().CreateClient(InvoiceReadHttpClientName);
        var readInvoicesResponse = await api1HttpClient.GetStringAsync("https://invoice-app.local/read-invoices", this._timeout.Token);
        Assert.Equal("This protected endpoint is for reading invoices", readInvoicesResponse);

        // Ensure the token is the same than the one we got after the background caching as it should still be valid
        var tokenAfterAuthenticatedRequest = await tokenCache.GetAsync(InvoiceReadHttpClientName, this._timeout.Token);
        Assert.NotNull(tokenAfterAuthenticatedRequest);
        Assert.Equal(tokenAfterStartupBackgroundCaching, tokenAfterAuthenticatedRequest);

        // Wait until token is expired - before that, there should be no background refresh
        var tokenManagementService = this._api1.Services.GetRequiredService<ClientCredentialsTokenManagementService>();
        Assert.Equal(0, tokenManagementService.BackgroundRefreshedTokenCount);
        var timeToLive = tokenAfterAuthenticatedRequest.GetTimeToLive(DateTimeOffset.UtcNow);
        testOutputHelper.WriteLine(timeToLive.ToString());
        await Task.Delay(timeToLive, this._timeout.Token);

        // At this point we're not making any new authenticated HTTP request,
        // but a background task should have refreshed the token, which should differ from the initially cached one
        Assert.Equal(1, tokenManagementService.BackgroundRefreshedTokenCount);
        var tokenAfterFirstBackgroundRefresh = await tokenCache.GetAsync(InvoiceReadHttpClientName, this._timeout.Token);
        Assert.NotNull(tokenAfterFirstBackgroundRefresh);
        Assert.NotEqual(tokenAfterStartupBackgroundCaching, tokenAfterFirstBackgroundRefresh);

        // If we wait a little bit longer, the token should be refreshed again
        await Task.Delay(tokenAfterFirstBackgroundRefresh.GetTimeToLive(DateTimeOffset.UtcNow), this._timeout.Token);
        Assert.Equal(2, tokenManagementService.BackgroundRefreshedTokenCount);
        var tokenAfterSecondBackgroundRefresh = await tokenCache.GetAsync(InvoiceReadHttpClientName, this._timeout.Token);
        Assert.NotNull(tokenAfterSecondBackgroundRefresh);
        Assert.NotEqual(tokenAfterFirstBackgroundRefresh, tokenAfterSecondBackgroundRefresh);
    }

    public async Task DisposeAsync()
    {
        if (this._api2 != null)
        {
            await this._api2.DisposeAsync();
        }

        if (this._api1 != null)
        {
            await this._api1.DisposeAsync();
        }

        if (this._idp != null)
        {
            await this._idp.DisposeAsync();
        }
    }
}