# GSoft.Authentication.Client.Credentials.Grant

[![nuget](https://img.shields.io/nuget/v/GSoft.Authentication.Client.Credentials.Grant.svg?logo=nuget)](https://www.nuget.org/packages/GSoft.Authentication.Client.Credentials.Grant/)
[![build](https://img.shields.io/github/actions/workflow/status/gsoft-inc/gsoft-authentication-client-credentials-grant/publish.yml?logo=github&branch=main)](https://github.com/gsoft-inc/gsoft-authentication-client-credentials-grant/actions/workflows/publish.yml)

## TODO

Welcome to your new scaffolded library. Make sure to review these steps before committing. Delete this section when you're done.

* Find all occurrences of `TODO` in generated files and replace them with what makes sense for your project.
* Make sure that generated URLs are correct (NuGet badges, project GitHub URL, etc.)
* Workflows ([GitHub actions](https://docs.github.com/en/actions)) are automatically registered so you don't have to do anything. These are the workflows:
  * `.github/workflows/ci.yml`: Build, run tests and create a NuGet package without publishing it on pull requests only.
  * `.github/workflows/publish.yml`: Build, run tests, and push the published package to your feed when committing on main branch or creating a `*.*.*` tag.
  * `.github/workflows/semgrep.yml`: Security analysis for your code on pull requests and on a weekly basis with [Semgrep](https://semgrep.dev/docs/cli-reference/).
* A [Renovate workflow from another gsoft-inc repository](https://github.com/gsoft-inc/gsoft-renovate-workflow) will take care of checking depencendies every day. This workflow will read this repository's `renovate.json` configuration. There is no need to add a `RENOVATE_TOKEN` secret, as it is already done is the other private repository.

What's included:

* Pipelines for pull request checks, continuous delivery package publishing, automated dependency updates, security checks.
* Class library targeting .NET Standard, a Xunit-based test project, both built with .NET 6 SDK.
* Public API breaking changes detection using [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md), which is also used in .NET and many other open-souce projects source code.
* Shared csproj properties with a `Directory.Build.props` file.
* A `Build.ps1` script that can be executed to simulate a CI build locally.
* A public `*.snk` key used to sign the assembly and make it [strong-named](https://learn.microsoft.com/en-us/dotnet/standard/assembly/strong-named). This is required by some ShareGate projects. It is also an industry standard: Microsoft, Polly, MediatR, Newtonsoft.Json, Moq, FluentAssertions, etc.
* [Source Link](https://github.com/dotnet/sourcelink) support.
* Issue templates, Apache License 2.0, markdown files for contributing and security required by GSec.


## Getting started

TODO


## Building, releasing and versioning

The project can be built by running `Build.ps1`. It uses [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) to help detect public API breaking changes. Use the built-in roslyn analyzer to ensure that public APIs are declared in `PublicAPI.Shipped.txt`, and obsolete public APIs in `PublicAPI.Unshipped.txt`.

A new *preview* NuGet package is **automatically published** on any new commit on the main branch. This means that by completing a pull request, you automatically get a new NuGet package.

When you are ready to **officially release** a stable NuGet package by following the [SemVer guidelines](https://semver.org/), simply **manually create a tag** with the format `x.y.z`. This will automatically create and publish a NuGet package for this version.


## License

Copyright © 2023, GSoft Group Inc. This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/gsoft-license/blob/master/LICENSE.
