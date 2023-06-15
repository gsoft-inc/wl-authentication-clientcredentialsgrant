# GSoft.Authentication.ClientCredentialsGrant

| Description                                           | Download link                                                                                                                                                                                                      | Build status                                                                                                                                                                                                                                                        |
|-------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Client-side library for any .NET application          | [![nuget](https://img.shields.io/nuget/v/GSoft.Extensions.Http.Authentication.ClientCredentialsGrant.svg?logo=nuget)](https://www.nuget.org/packages/GSoft.Extensions.Http.Authentication.ClientCredentialsGrant/) | [![build](https://img.shields.io/github/actions/workflow/status/gsoft-inc/gsoft-authentication-clientcredentialsgrant/publish.yml?logo=github&branch=main)](https://github.com/gsoft-inc/gsoft-authentication-clientcredentialsgrant/actions/workflows/publish.yml) |
| Server-side library for ASP.NET Core web applications | [![nuget](https://img.shields.io/nuget/v/GSoft.AspNetCore.Authentication.ClientCredentialsGrant.svg?logo=nuget)](https://www.nuget.org/packages/GSoft.AspNetCore.Authentication.ClientCredentialsGrant/)           | [![build](https://img.shields.io/github/actions/workflow/status/gsoft-inc/gsoft-authentication-clientcredentialsgrant/publish.yml?logo=github&branch=main)](https://github.com/gsoft-inc/gsoft-authentication-clientcredentialsgrant/actions/workflows/publish.yml) |

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
* Default authorization policies, but you can still create your own policies.
* Non-intrusive: default policies must be explicitly used, and the default authentication scheme can be modified.
* Support for ASP.NET Core 6.0 and later.

**Requirements and Considerations**:

* Your OAuth 2.0 authorization server **must expose its metadata** at the URL `<AUTHORITY>/.well-known/openid-configuration`, as described in [RFC 8414](https://www.rfc-editor.org/rfc/rfc8414.html#section-3).
* The client-side application uses [data protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction). It is important to note that your data protection configuration **should support distributed workloads if you have multiple instances of a client application**. [Microsoft recommends using a combination of Azure Key Vault and Azure Storage](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview) to ensure that data encrypted by an instance of a client application can be read by another instance.


## Getting started

### Client-side library

Install the package [GSoft.Extensions.Http.Authentication.ClientCredentialsGrant](https://www.nuget.org/packages/GSoft.Extensions.Http.Authentication.ClientCredentialsGrant/) in your client-side application
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
services.AddOptions<ClientCredentialsOptions>("MyClient").BindConfiguration(configSectionPath: "MyConfigSection");

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

_This client-side library is based on [Duende.AccessTokenManagement](https://github.com/DuendeSoftware/Duende.AccessTokenManagement/tree/1.1.0), Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0._


### Server-side library

Install the package [GSoft.AspNetCore.Authentication.ClientCredentialsGrant](https://www.nuget.org/packages/GSoft.AspNetCore.Authentication.ClientCredentialsGrant/) in your server-side ASP.NET Core application and register the authentication services:

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
        "Audience": "<audience>"
      }
    }
  }
}
```

Next, register the authorization services:

```csharp
builder.Services.AddAuthorization(options =>
{
    // Change the scheme here if you registered a custom scheme in the authentication services.
    // You can also add requirements to your policy, such as '.RequireClaim("name", "value", ["values"])'.
    options.AddPolicy("my-policy", x => x.AddAuthenticationSchemes(ClientCredentialsDefaults.AuthenticationScheme).RequireAuthenticatedUser());
});
```

Finally, register the authentication and authorization middlewares in your ASP.NET Core app and decorate your endpoints with the `AuthorizeAttribute`:

```csharp
var app = builder.Build();
// [...]

app.UseAuthentication();
app.UseAuthorization();

// Minimal APIs
app.MapGet("/hello-world", () => "Hello World!").RequireAuthorization("my-policy");

// Controller-style
[Authorize("my-policy")]
[HttpGet("hello-world")]
public IActionResult HelloWorld() => this.Ok("Hello world");
```


## Building, releasing and versioning

The project can be built by running `Build.ps1`. It uses [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) to help detect public API breaking changes. Use the built-in roslyn analyzer to ensure that public APIs are declared in `PublicAPI.Shipped.txt`, and obsolete public APIs in `PublicAPI.Unshipped.txt`.

A new *preview* NuGet package is **automatically published** on any new commit on the main branch. This means that by completing a pull request, you automatically get a new NuGet package.

When you are ready to **officially release** a stable NuGet package by following the [SemVer guidelines](https://semver.org/), simply **manually create a tag** with the format `x.y.z`. This will automatically create and publish a NuGet package for this version.

## License

Copyright © 2023, GSoft Group Inc. This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/gsoft-license/blob/master/LICENSE.