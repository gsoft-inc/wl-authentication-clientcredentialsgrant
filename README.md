# GSoft.Authentication.ClientCredentialsGrant

[![nuget](https://img.shields.io/nuget/v/GSoft.Authentication.ClientCredentialsGrant.svg?logo=nuget)](https://www.nuget.org/packages/GSoft.Authentication.ClientCredentialsGrant/)
[![build](https://img.shields.io/github/actions/workflow/status/gsoft-inc/gsoft-authentication-clientcredentialsgrant/publish.yml?logo=github&branch=main)](https://github.com/gsoft-inc/gsoft-authentication-clientcredentialsgrant/actions/workflows/publish.yml)

This library offers an IHttpClientBuilder extension method for streamlined access token retrieval and caching using the OAuth 2.0 Client Credentials Grant flow.

## Getting started

Install the package `GSoft.Authentication.ClientCredentialsGrant` in the project where you want to register an HttpClient.
This package contains the extension method that adds the access token management to an HttpClient.

## Example
```json
// appsettings.json
{
  "Service1": {
    "Authority": "<authority_url>",
    "ClientId": "<client_id>",
    "ClientSecret": "<client_secret>",
    "Scopes": [
      "scope1",
      "scope2"
    ]
  },
  "Service2": {
    "Authority": "<authority_url>",
    "ClientId": "<client_id>",
    "ClientSecret": "<client_secret>",
    "Scope": "scopeA scopeB"
  },
  "OtherService": {
    // ...
  }
}
```

```csharp
// Options configuration
services.AddOptions<ClientCredentialsOptions>(builder.Name)
    .BindConfiguration($"Service1");
services.AddOptions<ClientCredentialsOptions>(builder.Name)
    .BindConfiguration($"Service2");

// HttpClient registration
serivces.AddHttpClient("Service1").AddClientCredentialsHandler();
serivces.AddHttpClient("Service2").AddClientCredentialsHandler(options => {
    options.ClientId = "0cbc8ffa-cd51-48a1-8e21-cad7d008fc74" 
});

// Example how to make an authenticated call
internal sealed class MyCommandHandler
{
    private readonly HttpClient _httpClient;

    public MyCommandHandler(IHttpClientFactory httpClientFactory)
    {
        this._httpClient = httpClientFactory.CreateClient("Service2");
    }

    public async Task HandleAsync()
    {
        await this._httpClient.GetStringAsync("https://targetservice.com");
    }
}
```

## Building, releasing and versioning

The project can be built by running `Build.ps1`. It uses [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) to help detect public API breaking changes. Use the built-in roslyn analyzer to ensure that public APIs are declared in `PublicAPI.Shipped.txt`, and obsolete public APIs in `PublicAPI.Unshipped.txt`.

A new *preview* NuGet package is **automatically published** on any new commit on the main branch. This means that by completing a pull request, you automatically get a new NuGet package.

When you are ready to **officially release** a stable NuGet package by following the [SemVer guidelines](https://semver.org/), simply **manually create a tag** with the format `x.y.z`. This will automatically create and publish a NuGet package for this version.

## License

Copyright © 2023, GSoft Group Inc. This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/gsoft-license/blob/master/LICENSE.
