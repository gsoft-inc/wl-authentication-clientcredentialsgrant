using System.Collections.Concurrent;
using System.Net;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;
using Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Secret = Duende.IdentityServer.Models.Secret;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests;

public class IntegrationTests
{
    private const string Audience = "invoices";

    private readonly ITestOutputHelper _testOutputHelper;

    public IntegrationTests(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Real_Client_Server_Communication()
    {
        // Define some OAuth 2.0 scopes for fictional invoices access management
        var identityApiScopes = new[]
        {
            new ApiScope($"{Audience}:read", "Reads your invoices."),
            new ApiScope($"{Audience}:pay", "Pays your invoices."),
        };

        // Define the protected resources, here an invoice API (represents something we want to communicate with)
        var identityApiResources = new[]
        {
            new ApiResource(Audience, "Invoice API") { Scopes = { $"{Audience}:read", $"{Audience}:pay" } },
        };

        var tokenLifetime = TimeSpan.FromSeconds(12);
        var tokenCacheLifetimeBuffer = TimeSpan.FromSeconds(3); // token will be evicted from cache prior to its expiration

        // Define the OAuth 2.0 clients and the scopes that can be granted
        var identityOAuthClients = new[]
        {
            // This client only allows to read invoices
            new Client
            {
                ClientId = "invoices_read_client",
                ClientSecrets = new[] { new Secret("invoices_read_client_secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = { $"{Audience}:read" },
                AccessTokenLifetime = (int)tokenLifetime.TotalSeconds,
            },
        };

        // Build a real but in-memory ASP.NET Core test server that will both act as identity provider (using IdentityServer) and as the protected API that we'll try to access using a authenticated HttpClient
        var webAppBuilder = WebApplication.CreateBuilder();
        webAppBuilder.WebHost.UseTestServer(x => x.BaseAddress = new Uri("https://identity.local", UriKind.Absolute));

        // Here begins services registrations in the dependency injection container
        webAppBuilder.Services.AddLogging(x => x.SetMinimumLevel(LogLevel.Debug).ClearProviders().AddProvider(new XunitLoggerProvider(this._testOutputHelper)));
        webAppBuilder.Services.AddSingleton<TestServer>(x => (TestServer)x.GetRequiredService<IServer>());
        webAppBuilder.Services.AddSingleton<TestServerHandler>();
        webAppBuilder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

        webAppBuilder.Services.AddIdentityServer()
            .AddInMemoryClients(identityOAuthClients)
            .AddInMemoryApiResources(identityApiResources)
            .AddInMemoryApiScopes(identityApiScopes)
            .AddSigningKeyStore<InMemorySigningKeyStore>();

        // Create the authorization policy that will be used to protect our invoices endpoints
        webAppBuilder.Services.AddAuthentication().AddClientCredentials();
        webAppBuilder.Services.AddOptions<JwtBearerOptions>(ClientCredentialsDefaults.AuthenticationScheme).Configure<TestServerHandler>((options, testServerClient) =>
        {
            options.Audience = Audience;
            options.Authority = "https://identity.local";
            options.BackchannelHttpHandler = testServerClient;
        });

        // This invoice authorization policy must be individually applied to endpoints
        webAppBuilder.Services.AddClientCredentialsAuthorization();

        // Change the primary HTTP message handler of this library to communicate with this in-memory test server without accessing the network
        webAppBuilder.Services.AddHttpClient(ClientCredentialsConstants.BackchannelHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(x => x.GetRequiredService<TestServer>().CreateHandler());

        // Configure the authenticated HttpClient used to communicate with the protected invoices endpoint
        // Also change the primary HTTP message handler to communicate with this in-memory test server without accessing the network
        const string invoiceReadClientName = "invoices_read_http_client";
        webAppBuilder.Services.AddHttpClient(invoiceReadClientName)
            .ConfigurePrimaryHttpMessageHandler(x => x.GetRequiredService<TestServer>().CreateHandler())
            .AddClientCredentialsHandler(options =>
            {
                options.Authority = "https://identity.local";
                options.ClientId = "invoices_read_client";
                options.ClientSecret = "invoices_read_client_secret";
                options.Scope = $"{Audience}:read";
                options.CacheLifetimeBuffer = tokenCacheLifetimeBuffer;
            });

        // Here begins ASP.NET Core middleware pipelines registration
        var webApp = webAppBuilder.Build();

        webApp.UseIdentityServer();
        webApp.UseAuthorization();

        webApp.MapGet("/public", () => "This endpoint is public").RequireHost("invoice-app.local");
        webApp.MapGet("/read-invoices", () => "This protected endpoint is for reading invoices").RequireAuthorization(ClientCredentialsDefaults.AuthorizationReadPolicy).RequireHost("invoice-app.local");
        webApp.MapGet("/pay-invoices", () => "This protected endpoint is for paying invoices").RequireAuthorization(ClientCredentialsDefaults.AuthorizationWritePolicy).RequireHost("invoice-app.local");
        webApp.MapGet("/read-invoices-granular", () => "This protected endpoint is for reading invoices").RequireClientCredentials("read").RequireHost("invoice-app.local");
        webApp.MapGet("/pay-invoices-granular", () => "This protected endpoint is for paying invoices").RequireClientCredentials("pay").RequireHost("invoice-app.local");

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        try
        {
            // Start the web app without blocking, the cancellation token source will make sure it will be shutdown if something wrong happens
            webApp.RunAsync(cts.Token).Forget();

            try
            {
                // Ensure that registered clients get cached tokens on app startup
                var cachingBackgroundService = webApp.Services.GetRequiredService<OnStartupTokenCacheBackgroundService>();
                await cachingBackgroundService.WaitForTokenCachingToCompleteAsync(cts.Token);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                throw new TimeoutException($"{nameof(OnStartupTokenCacheBackgroundService)} didn't complete its job in time");
            }

            // Retrieve the access token from the cache for later comparison
            // Assert that the token lifetime is set according to the IdentityServer configuration
            var tokenCache = webApp.Services.GetRequiredService<IClientCredentialsTokenCache>();
            var tokenAfterStartupBackgroundCaching = await tokenCache.GetAsync(invoiceReadClientName, cts.Token);
            Assert.NotNull(tokenAfterStartupBackgroundCaching);
            Assert.InRange(tokenAfterStartupBackgroundCaching.Expiration, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.Add(tokenLifetime));

            var invoicesReadHttpClient = webApp.Services.GetRequiredService<IHttpClientFactory>().CreateClient(invoiceReadClientName);

            // Consuming an anonymous/public endpoint should work
            var publicEndpointResponse = await invoicesReadHttpClient.GetStringAsync("https://invoice-app.local/public", cts.Token);
            Assert.Equal("This endpoint is public", publicEndpointResponse);

            // Using the classic policy, reading invoices should be successful because we're authenticated with a JWT that has the "invoices" audience and "invoices.read" scope
            var readInvoicesResponse = await invoicesReadHttpClient.GetStringAsync("https://invoice-app.local/read-invoices", cts.Token);
            Assert.Equal("This protected endpoint is for reading invoices", readInvoicesResponse);
            
            // Using the granular policy, reading invoices should be successful because we're authenticated with a JWT that has the "invoices" audience and "invoices.read" scope
            var readInvoicesGranularResponse = await invoicesReadHttpClient.GetStringAsync("https://invoice-app.local/read-invoices-granular", cts.Token);
            Assert.Equal("This protected endpoint is for reading invoices", readInvoicesGranularResponse);

            // Using the classic policy, paying invoices should throw a forbidden HTTP exception because the JWT doesn't have the "invoices.pay" scope
            var payInvoicesException = await Assert.ThrowsAsync<HttpRequestException>(() => invoicesReadHttpClient.GetStringAsync("https://invoice-app.local/pay-invoices", cts.Token));
            Assert.Equal(HttpStatusCode.Forbidden, payInvoicesException.StatusCode);

            // Using the granular policy, paying invoices should throw a forbidden HTTP exception because the JWT doesn't have the "invoices.pay" scope
            var payInvoicesGranularException = await Assert.ThrowsAsync<HttpRequestException>(() => invoicesReadHttpClient.GetStringAsync("https://invoice-app.local/pay-invoices-granular", cts.Token));
            Assert.Equal(HttpStatusCode.Forbidden, payInvoicesGranularException.StatusCode);

            // We require JWT-authenticated requests to be sent over HTTPS
            var unsecuredException = await Assert.ThrowsAsync<ClientCredentialsException>(() => invoicesReadHttpClient.GetStringAsync("http://invoice-app.local/public", cts.Token));
            Assert.Equal("Due to security concerns, authenticated requests must be sent over HTTPS", unsecuredException.Message);

            // Ensure the token is the same than the one we got after the background caching as it should still be valid
            var tokenAfterAuthenticatedRequest = await tokenCache.GetAsync(invoiceReadClientName, cts.Token);
            Assert.NotNull(tokenAfterAuthenticatedRequest);
            Assert.Equal(tokenAfterStartupBackgroundCaching, tokenAfterAuthenticatedRequest);

            // Wait until token is expired - before that, there should be no background refresh
            var tokenManagementService = webApp.Services.GetRequiredService<ClientCredentialsTokenManagementService>();
            Assert.Equal(0, tokenManagementService.BackgroundRefreshedTokenCount);
            await Task.Delay(tokenAfterAuthenticatedRequest.Expiration - DateTimeOffset.UtcNow, cts.Token);

            // At this point we're not making any new authenticated HTTP request,
            // but a background task should have refreshed the token, which should differ from the initially cached one
            Assert.Equal(1, tokenManagementService.BackgroundRefreshedTokenCount);
            var tokenAfterFirstBackgroundRefresh = await tokenCache.GetAsync(invoiceReadClientName, cts.Token);
            Assert.NotNull(tokenAfterFirstBackgroundRefresh);
            Assert.NotEqual(tokenAfterStartupBackgroundCaching, tokenAfterFirstBackgroundRefresh);

            // If we wait a little bit longer, the token should be refreshed again
            await Task.Delay(tokenAfterFirstBackgroundRefresh.Expiration - DateTimeOffset.UtcNow, cts.Token);
            Assert.Equal(2, tokenManagementService.BackgroundRefreshedTokenCount);
            var tokenAfterSecondBackgroundRefresh = await tokenCache.GetAsync(invoiceReadClientName, cts.Token);
            Assert.NotNull(tokenAfterSecondBackgroundRefresh);
            Assert.NotEqual(tokenAfterFirstBackgroundRefresh, tokenAfterSecondBackgroundRefresh);
        }
        finally
        {
            // Shut down the web app
#pragma warning disable CA1849 // Given that we are using .Net6/7/8, updating the method to CancelAsync would break backwards compatibility
            cts.Cancel();
#pragma warning restore CA1849
        }
    }

    private sealed class TestServerHandler : DelegatingHandler
    {
        private readonly TestServer _testServer;
        private HttpClient? _testServerClient;

        public TestServerHandler(TestServer testServer)
        {
            this._testServer = testServer;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this._testServerClient ??= this._testServer.CreateClient();

            // Request has to be cloned since it has already gone through the httpclient wrapping this handler even though the request hasn't been sent yet.
            var cloneRequest = await CloneHttpRequest(request, cancellationToken);

            return await this._testServerClient.SendAsync(cloneRequest, cancellationToken);
        }

        private static async Task<HttpRequestMessage> CloneHttpRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cloneRequest = new HttpRequestMessage(request.Method, request.RequestUri);
            cloneRequest.Version = request.Version;

            foreach (var (key, value) in request.Headers)
            {
                cloneRequest.Headers.TryAddWithoutValidation(key, value);
            }

            foreach (var (key, value) in request.Options)
            {
                cloneRequest.Options.TryAdd(key, value);
            }

            if (request.Content != null)
            {
                var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                cloneRequest.Content = new ByteArrayContent(contentBytes);

                foreach (var (key, value) in request.Content.Headers)
                {
                    cloneRequest.Content.Headers.TryAddWithoutValidation(key, value);
                }
            }

            return cloneRequest;
        }
    }

    // Prevents IdentityServer from using the actual file system to store the signing keys
    // It also reduces the amount of logs, which makes troubleshooting easier
    private sealed class InMemorySigningKeyStore : ISigningKeyStore
    {
        private readonly ConcurrentDictionary<string, SerializedKey> _keys = new(StringComparer.Ordinal);

        public Task<IEnumerable<SerializedKey>> LoadKeysAsync()
        {
            return Task.FromResult(this._keys.Values.ToArray().AsEnumerable());
        }

        public Task StoreKeyAsync(SerializedKey key)
        {
            this._keys[key.Id] = key;
            return Task.CompletedTask;
        }

        public Task DeleteKeyAsync(string id)
        {
            this._keys.TryRemove(id, out _);
            return Task.CompletedTask;
        }
    }
}