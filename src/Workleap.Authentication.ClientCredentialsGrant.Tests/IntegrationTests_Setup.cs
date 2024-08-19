using System.Collections.Concurrent;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;
using Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Secret = Duende.IdentityServer.Models.Secret;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests;

public sealed partial class IntegrationTests
{
    private const string InvoicesAudience = "invoices";

    private const string InvoicesReadScope = $"{InvoicesAudience}:read";
    private const string InvoicesPayScope = $"{InvoicesAudience}:pay";

    private const string InvoicesReadClientId = "invoices_read_client";
    private const string InvoicesReadClientSecret = "invoices_read_client_secret";

    private const string InvoicesAuthority = "https://identity.local";

    private const string InvoiceReadHttpClientName = "invoices_read_http_client";

    // Tokens will be evicted from cache prior to their expiration
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan TokenCacheLifetimeBuffer = TimeSpan.FromSeconds(3);

    private WebApplication CreateTestIdentityProvider()
    {
        // Define some OAuth 2.0 scopes for fictional invoices access management
        ApiScope[] identityApiScopes =
        [
            new ApiScope(InvoicesReadScope, "Reads your invoices."),
            new ApiScope(InvoicesPayScope, "Pays your invoices.")
        ];

        // Define the protected resources, here an invoice API (represents something we want to communicate with)
        ApiResource[] identityApiResources =
        [
            new ApiResource(InvoicesAudience, "Invoice API")
            {
                Scopes = [InvoicesReadScope, InvoicesPayScope]
            }
        ];

        // Define the OAuth 2.0 clients and the scopes that can be granted
        Client[] identityOAuthClients =
        [
            // This client only allows to read invoices
            new Client
            {
                ClientId = InvoicesReadClientId,
                ClientSecrets = [new Secret(InvoicesReadClientSecret.Sha256())],
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = [InvoicesReadScope],
                AccessTokenLifetime = (int)TokenLifetime.TotalSeconds,
            }
        ];

        // Build a real but in-memory ASP.NET Core test server that will both act as identity provider (using IdentityServer) and as the protected API that we'll try to access using a authenticated HttpClient
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer(x => x.BaseAddress = new Uri(InvoicesAuthority, UriKind.Absolute));

        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new XunitLoggerProvider(testOutputHelper, "idp"));

        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

        builder.Services.AddIdentityServer()
            .AddInMemoryClients(identityOAuthClients)
            .AddInMemoryApiResources(identityApiResources)
            .AddInMemoryApiScopes(identityApiScopes)
            .AddSigningKeyStore<InMemorySigningKeyStore>();

        var idp = builder.Build();

        idp.Use(async (context, next) =>
        {
            // https://identityserver4.readthedocs.io/en/latest/endpoints/token.html#example
            const string identityServerTokenEndpoint = "/connect/token";

            if (context.Request.Path == identityServerTokenEndpoint)
            {
                Interlocked.Increment(ref this._tokenRequestCount);
            }

            await next(context);
        });

        idp.UseIdentityServer();

        return idp;
    }

    private WebApplication CreateTestApi(CreateApiOptions createApiOptions)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new XunitLoggerProvider(testOutputHelper, createApiOptions.AppName));

        builder.Services.AddSingleton<TestServer>(x => (TestServer)x.GetRequiredService<IServer>());
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

        // Create the authorization policy that will be used to protect our invoices endpoints
        builder.Services.AddAuthentication().AddClientCredentials(options =>
        {
            options.Audience = InvoicesAudience;
            options.Authority = InvoicesAuthority;
            options.Backchannel = createApiOptions.IdentityProvider.GetTestClient();
        });

        // This invoice authorization policy must be individually applied to endpoints
        builder.Services.AddClientCredentialsAuthorization();

        // Change the primary HTTP message handler of this library to communicate with the in-memory IDP server
        builder.Services.AddHttpClient(ClientCredentialsConstants.BackchannelHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => createApiOptions.IdentityProvider.GetTestServer().CreateHandler());

        // Configure the authenticated HttpClient used to communicate with the protected invoices endpoint
        // Also change the primary HTTP message handler to communicate with this in-memory test server without accessing the network
        builder.Services.AddHttpClient(InvoiceReadHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(x => x.GetRequiredService<TestServer>().CreateHandler())
            .AddClientCredentialsHandler(options =>
            {
                options.Authority = InvoicesAuthority;
                options.ClientId = InvoicesReadClientId;
                options.ClientSecret = InvoicesReadClientSecret;
                options.Scope = InvoicesReadScope;
                options.CacheLifetimeBuffer = TokenCacheLifetimeBuffer;
                options.EnforceHttps = createApiOptions.EnforceHttps;
            });

        // Share the same distributed cache among all instances of the test APIs
        builder.Services.AddSingleton<IDistributedCache>(this._sharedDistributedCache);

        // Here begins ASP.NET Core middleware pipelines registration
        var api = builder.Build();

        api.UseAuthorization();

        api.MapGet("/anonymous", () => "This endpoint is public")
            .RequireHost("invoice-app.local");

        api.MapGet("/read-invoices", () => "This protected endpoint is for reading invoices")
            .RequireAuthorization(ClientCredentialsDefaults.AuthorizationReadPolicy)
            .RequireHost("invoice-app.local");

        api.MapGet("/pay-invoices", () => "This protected endpoint is for paying invoices")
            .RequireAuthorization(ClientCredentialsDefaults.AuthorizationWritePolicy)
            .RequireHost("invoice-app.local");

        api.MapGet("/read-invoices-granular", () => "This protected endpoint is for reading invoices")
            .RequireClientCredentials("read")
            .RequireHost("invoice-app.local");

        api.MapGet("/pay-invoices-granular", () => "This protected endpoint is for paying invoices")
            .RequireClientCredentials("pay")
            .RequireHost("invoice-app.local");

        return api;
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

    private sealed class CreateApiOptions(WebApplication identityProvider)
    {
        public WebApplication IdentityProvider { get; } = identityProvider;

        public required string AppName { get; init; }

        public required bool EnforceHttps { get; init; }
    }
}