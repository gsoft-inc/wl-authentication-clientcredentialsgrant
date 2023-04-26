// This file is based on https://github.com/DuendeSoftware/Duende.AccessTokenManagement/blob/1.1.0/src/Duende.AccessTokenManagement/ClientCredentialsTokenEndpointService.cs
// Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0. All rights reserved.
//
// The original file has been significantly modified, and these modifications are Copyright (c) GSoft Group Inc., 2023.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Net;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

/// <summary>
/// Service responsible for retrieving a client credentials grant-based access token from an OAuth 2.0 identity provider.
/// </summary>
internal class ClientCredentialsTokenEndpointService : IClientCredentialsTokenEndpointService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ClientCredentialsOptions> _optionsMonitor;
    private readonly IOpenIdConfigurationRetriever _oidcRetriever;

    public ClientCredentialsTokenEndpointService(IHttpClientFactory httpClientFactory, IOptionsMonitor<ClientCredentialsOptions> optionsMonitor, IOpenIdConfigurationRetriever oidcRetriever)
    {
        this._httpClientFactory = httpClientFactory;
        this._optionsMonitor = optionsMonitor;
        this._oidcRetriever = oidcRetriever;
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

        // Eventually replace IdentityModel with Microsoft.Identity.Client (MSAL) when their generic authority feature is mature
        var response = await httpClient.RequestClientCredentialsTokenAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            ThrowOnIdentityModelError(clientName, response);

            return new ClientCredentialsToken
            {
                AccessToken = response.AccessToken,
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
        var exceptionMessagePrefix = $"An error occured while retrieving token for client '{clientName}'";

        // Checking this error type first helps us preserve the original exception
        if (response is { ErrorType: ResponseErrorType.Exception, Exception: { } exception })
        {
            throw new ClientCredentialsException(exceptionMessagePrefix, exception);
        }

        // TokenResponse.IsError and TokenResponse.Error implementations are incomplete but we handle the missing use cases below
        // https://github.com/IdentityModel/IdentityModel/blob/6.0.0/src/Client/Messages/ProtocolResponse.cs#L190
        if (response.IsError)
        {
            throw new ClientCredentialsException(exceptionMessagePrefix + ": " + response.Error);
        }

        switch (response.ErrorType)
        {
            case ResponseErrorType.Http when response.HttpStatusCode != default:
                throw new ClientCredentialsException(exceptionMessagePrefix + ": HTTP error " + (int)response.HttpStatusCode);
            case ResponseErrorType.Protocol:
                throw new ClientCredentialsException(exceptionMessagePrefix + ": HTTP error " + (int)HttpStatusCode.BadRequest);
        }

        if (string.IsNullOrEmpty(response.AccessToken))
        {
            throw new ClientCredentialsException(exceptionMessagePrefix + ": result was empty");
        }
    }
}