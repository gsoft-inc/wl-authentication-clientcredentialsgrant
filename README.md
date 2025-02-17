# Workleap.Authentication.ClientCredentialsGrant

| Description                                           | Download link                                                                                                                                                                                                      | Build status                                                                                                                                                                                                                                                  |
|-------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Client-side library for any .NET application          | [![nuget](https://img.shields.io/nuget/v/Workleap.Extensions.Http.Authentication.ClientCredentialsGrant.svg?logo=nuget)](https://www.nuget.org/packages/Workleap.Extensions.Http.Authentication.ClientCredentialsGrant/) | [![build](https://img.shields.io/github/actions/workflow/status/workleap/wl-authentication-clientcredentialsgrant/publish.yml?logo=github&branch=main)](https://github.com/workleap/wl-authentication-clientcredentialsgrant/actions/workflows/publish.yml) |
| Server-side library for ASP.NET Core web applications | [![nuget](https://img.shields.io/nuget/v/Workleap.AspNetCore.Authentication.ClientCredentialsGrant.svg?logo=nuget)](https://www.nuget.org/packages/Workleap.AspNetCore.Authentication.ClientCredentialsGrant/)           | [![build](https://img.shields.io/github/actions/workflow/status/workleap/wl-authentication-clientcredentialsgrant/publish.yml?logo=github&branch=main)](https://github.com/workleap/wl-authentication-clientcredentialsgrant/actions/workflows/publish.yml) |

This set of two libraries enables **authenticated machine-to-machine HTTP communication** between a .NET application and an ASP.NET Core web application.
HTTP requests are authenticated with JSON web tokens (JWT) **issued by an OAuth 2.0 authorization server** using [the client credentials grant flow](https://www.rfc-editor.org/rfc/rfc6749#section-4.4).

```
                            ┌────────────────────────────────┐
                 ┌─────────►│ OAuth 2.0 authorization server │◄──────────┐
                 │          └────────────────────────────────┘           │
                 │                                                       │
                 │ get token with                       get signing keys │
                 │ client credentials grant flow                         │ validate
                 │                                                       │    token
               ┌─┴───────────┐                           ┌───────────────┴────────┐
               │ Client .NET ├──────────────────────────►│ Protected ASP.NET Core │
               │ application │  authenticated HTTP call  │         service        │
               └─────────────┘                           └────────────────────────┘
```

The **client-side library** includes:

* Automatic acquisition and lifetime management of client credentials-based access tokens.
* Optimized access token caching with two layers of cache using [IMemoryCache](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory), [IDistributedCache](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed), and [data protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction) for encryption.
* Built-in customizable [retry policy](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly) for production-grade resilient HTTP requests made to the OAuth 2.0 authorization server.
* Built as an extension for the [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http/) library.
* Support for [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0).

The **server-side library** includes:

* JWT authentication using the [Microsoft.AspNetCore.Authentication.JwtBearer](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer) library.
* Authorization attribute and policy to easily enforce granular scopes on your endpoints.
* Authorization attribute and policy to easily enforce classic Workleap permissions (read, write, admin).
* Support of OpenAPI security definition and security requirement generation when using Swashbuckle.
* Non-intrusive: default policies must be explicitly used, and the default authentication scheme can be modified.
* Support for ASP.NET Core 6.0 and later.

**Requirements and Considerations**:

* Your OAuth 2.0 authorization server **must expose its metadata** at the URL `<AUTHORITY>/.well-known/openid-configuration`, as described in [RFC 8414](https://www.rfc-editor.org/rfc/rfc8414.html#section-3).
* The client-side application uses [data protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction). It is important to note that your data protection configuration **should support distributed workloads if you have multiple instances of a client application**. [Microsoft recommends using a combination of Azure Key Vault and Azure Storage](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview) to ensure that data encrypted by an instance of a client application can be read by another instance.


## Getting started

### Client-side library

Install the package [Workleap.Extensions.Http.Authentication.ClientCredentialsGrant](https://www.nuget.org/packages/Workleap.Extensions.Http.Authentication.ClientCredentialsGrant/) in your client-side application
that needs to communicate with the protected ASP.NET Core server. Then, use one of the following methods to configure an authenticated `HttpClient`:

```csharp
// Method 1: directly set the options values with C# code
services.AddHttpClient("MyClient").AddClientCredentialsHandler(options =>
{
    options.Authority = "<oauth2_authorization_server_base_url>";
    options.ClientId = "<oauth2_client_id>";
    options.ClientSecret = "<oauth2_client_secret>"; // use a secret store instead of hardcoding the value
    options.Scope = "<optional_requested_scope>"; // use "Scopes" for multiple values
});

// Method 2: bind the options to a configuration section
services.AddHttpClient("MyClient").AddClientCredentialsHandler(configuration.GetRequiredSection("MyConfigSection").Bind);

// Method 3: Lazily bind the options to a configuration section
services.AddHttpClient("MyClient").AddClientCredentialsHandler();
services.AddOptions<ClientCredentialsOptions>("MyClient").Bind(configuration.GetRequiredSection("MyConfigSection"));

// appsettings.json:
{
  "MyConfigSection": {
    "Authority": "<oauth2_authorization_server_base_url>",
    "ClientId": "<oauth2_client_id>",
    "ClientSecret": "<oauth2_client_secret>", // use a secret configuration provider instead of hardcoding the value
    "Scope": "<optional_requested_scope>", // use "Scopes" for multiple values,
    "EnforceHttps": "<boolean>", // use EnforceHttps to force all authenticated to be sent via https
  }
}

// You can also use the generic HttpClient registration with any of these methods:
services.AddHttpClient<MyClient>().AddClientCredentialsHandler( /* [...] */);
```

Note on `EnforceHttps`.
It is possible to allow http authenticated requests, however, this should be limited to exceptional scenarios.
It is strongly advised that you always use https for authenticated requests transmitted as the token sent will be in clear.

Then, instantiate the `HttpClient` later on using `IHttpClientFactory` or directly inject it in the constructor if you used the generic registration:

```csharp
public class MyClient
{
    private readonly HttpClient _httpClient;

    public MyClient(IHttpClientFactory httpClientFactory)
    {
        this._httpClient = httpClientFactory.CreateClient("MyClient");
    }

    public async Task DoSomeAuthenticatedHttpCallAsync()
    {
        await this._httpClient.GetStringAsync("https://myservice");
    }
}
```

Starting from version 1.3.0, tokens are pre-fetched and cached at app startup.
Subsequently, there is a periodic refresh of the token before its expiration and cache eviction.
This behavior can be disabled by setting `ClientCredentialsOptions.EnablePeriodicTokenBackgroundRefresh` to `false`.

_This client-side library is based on [Duende.AccessTokenManagement](https://github.com/DuendeSoftware/Duende.AccessTokenManagement/tree/1.1.0), Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0._


### Server-side library

The server-side library add the `RequireClientCredentials` attribute that simplify the use of the client credentials flow in your ASP.NET Core application:
- Simply specify the required permissions in the attribute (e.g: `[RequireClientCredentials("read")`]
- Support multiple claims types (e.g: `scope`, `scp`, `http://schemas.microsoft.com/identity/claims/scope`)
- Support multiple claims format (e.g: `read`, `{Audience}:read`)

Install the package [Workleap.AspNetCore.Authentication.ClientCredentialsGrant](https://www.nuget.org/packages/Workleap.AspNetCore.Authentication.ClientCredentialsGrant/) in your server-side ASP.NET Core application and register the authentication services:

```csharp
// Registers Microsoft's JwtBearer handler with a default "ClientCredentials" authentication scheme.
// This authentication scheme can be changed using other methods overloads.
builder.Services.AddAuthentication().AddClientCredentials();
```

This will automatically bind the configuration section `Authentication:Schemes:ClientCredentials` (unless you've changed the authentication scheme).
For instance, the example above works well with this `appsettings.json`:

```json
{
  "Authentication": {
    "Schemes": {
      "ClientCredentials": {
        "Authority": "<oauth2_authorization_server_base_url>",
        "Audience": "<audience>",
        "MetadataAddress": "<oauth2_authorization_server_metadata_address>"
      }
    }
  }
}
```

Next, protect your endpoints with the `RequireClientCredentials` attribute:

```csharp
// When using Controlled-Based
[HttpGet]
[Route("weather")]
[RequireClientCredentials("read")]
public async Task<IActionResult> GetWeather()
{...}

// When using Minimal APIs
app.MapGet("/weather", () => {...}).RequireClientCredentials("read");
```

Next, register the authorization services which all the required authorization policies:

```csharp
builder.Services
    .AddClientCredentialsAuthorization();
```

Finally, register the authentication and authorization middlewares in your ASP.NET Core app.

```csharp
var app = builder.Build();
// [...]

app.UseAuthentication();
app.UseAuthorization();

// [...] Map your endpoints
```

#### OpenAPI integration

If you are using [Swashbuckle](https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger) to document your API, the `[RequireClientCredentials]` attribute will automatically populate the security definitions and requirements in the OpenAPI specification. For minimal APIs, there is a corresponding `RequireClientCredentials()` method. 

For example:

```csharp
// Controlled-based approach
[HttpGet]
[Route("weather")]
[RequireClientCredentials("read")]
public async Task<IActionResult> GetWeather()
{ /* ... */ }

// Minimal APIs
app.MapGet("/weather", () => { /* ... */ }).RequireClientCredentials("read");
```

Will generate this:

```yaml
paths:
  /weather:
    get:
      summary: 'Required scope: read.'
      responses:
        '200':
          description: OK
        '401':
          description: Unauthorized
        '403':
          description: Forbidden
      security:
        - clientcredentials:
            - target-entity:b108bbc9-538e-403b-9faf-e5cd874eb17f:read # Based on the provided JwtBearerOptions.Audience
components:
  securitySchemes:
    clientcredentials:
      type: oauth2
      flows:
        clientCredentials:
          tokenUrl: https://localhost:9020/oauth2/token # Based on provided ClientCredentials.Authority
          scopes:
            target-entity:b108bbc9-538e-403b-9faf-e5cd874eb17f: Request all permissions for specified client ID
            target-entity:b108bbc9-538e-403b-9faf-e5cd874eb17f:read: Request permission 'read' for specified client ID
```


## Building, releasing and versioning

The project can be built by running `Build.ps1`. It uses [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) to help detect public API breaking changes. Use the built-in roslyn analyzer to ensure that public APIs are declared in `PublicAPI.Shipped.txt`, and obsolete public APIs in `PublicAPI.Unshipped.txt`.

A new *preview* NuGet package is **automatically published** on any new commit on the main branch. This means that by completing a pull request, you automatically get a new NuGet package.

When you are ready to **officially release** a stable NuGet package by following the [SemVer guidelines](https://semver.org/), simply **manually create a tag** with the format `x.y.z`. This will automatically create and publish a NuGet package for this version.

## License

Copyright © 2023, Workleap. This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/workleap/gsoft-license/blob/master/LICENSE.