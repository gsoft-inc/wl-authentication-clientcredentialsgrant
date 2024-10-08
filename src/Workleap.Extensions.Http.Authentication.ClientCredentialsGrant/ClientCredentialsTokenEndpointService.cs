﻿// This file is based on https://github.com/DuendeSoftware/Duende.AccessTokenManagement/blob/1.1.0/src/Duende.AccessTokenManagement/ClientCredentialsTokenEndpointService.cs
// Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0. All rights reserved.
//
// The original file has been significantly modified, and these modifications are Copyright (c) Workleap, 2023.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Text;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

/// <summary>
/// Service responsible for retrieving a client credentials grant-based access token from an OAuth 2.0 identity provider.
/// </summary>
internal class ClientCredentialsTokenEndpointService : IClientCredentialsTokenEndpointService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ClientCredentialsOptions> _optionsMonitor;
    private readonly IOpenIdConfigurationRetriever _oidcRetriever;
    private readonly ILogger<ClientCredentialsTokenEndpointService> _logger;

    public ClientCredentialsTokenEndpointService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ClientCredentialsOptions> optionsMonitor,
        IOpenIdConfigurationRetriever oidcRetriever,
        ILogger<ClientCredentialsTokenEndpointService> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._optionsMonitor = optionsMonitor;
        this._oidcRetriever = oidcRetriever;
        this._logger = logger;
    }

    public async Task<ClientCredentialsToken> RequestTokenAsync(string clientName, CancellationToken cancellationToken)
    {
        var options = this._optionsMonitor.Get(clientName);

        var metadataEndpoint = await this._oidcRetriever.GetAsync(options.Authority, cancellationToken).ConfigureAwait(false);

        var request = new ClientCredentialsTokenRequest
        {
            Address = metadataEndpoint.TokenEndpoint,
            Scope = options.Scope,
            ClientId = options.ClientId,
            ClientSecret = options.ClientSecret,
            ClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader,
        };

        var httpClient = this._httpClientFactory.CreateClient(ClientCredentialsConstants.BackchannelHttpClientName);

        this._logger.RequestingNewTokenForClient(options.ClientId);

        // Eventually replace IdentityModel with Microsoft.Identity.Client (MSAL)
        // https://anthonysimmon.com/replacing-identitymodel-with-msal-oidc-support/
        var response = await httpClient.RequestClientCredentialsTokenAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            ThrowOnIdentityModelError(clientName, response);

            return new ClientCredentialsToken
            {
                AccessToken = response.AccessToken!,
                Expiration = response.ExpiresIn == 0 ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn),
            };
        }
        finally
        {
            try
            {
                response.HttpResponse?.Dispose();
            }
            catch
            {
                // That's fine, we did everything we could to make sure the underlying HTTP response was disposed
            }
        }
    }

    private static void ThrowOnIdentityModelError(string clientName, TokenResponse response)
    {
        if (response.IsError)
        {
            throw new ClientCredentialsException(GetErrorMessage(clientName, response), response.Exception);
        }

        if (string.IsNullOrEmpty(response.AccessToken))
        {
            throw new ClientCredentialsException(GetErrorMessage(clientName, response, "result was empty"));
        }
    }

    private static string GetErrorMessage(string clientName, TokenResponse response, string? additionalMessagePart = null)
    {
        var exceptionMessagePrefixBuilder = new StringBuilder($"An error occurred while retrieving token for client '{clientName}'");

        if (response.Error != null)
        {
            exceptionMessagePrefixBuilder.Append($": {response.Error}");
        }

        if (response.ErrorDescription != null)
        {
            exceptionMessagePrefixBuilder.Append($": {response.ErrorDescription}");
        }

        if (additionalMessagePart != null)
        {
            exceptionMessagePrefixBuilder.Append($": {additionalMessagePart}");
        }

        return exceptionMessagePrefixBuilder.ToString();
    }
}