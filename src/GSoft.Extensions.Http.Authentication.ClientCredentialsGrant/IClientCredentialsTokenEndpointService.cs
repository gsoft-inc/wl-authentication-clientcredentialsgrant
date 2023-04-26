// This file is based on https://github.com/DuendeSoftware/Duende.AccessTokenManagement/blob/1.1.0/src/Duende.AccessTokenManagement/Interfaces/IClientCredentialsTokenEndpointService.cs
// Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0. All rights reserved.
//
// The original file has been significantly modified, and these modifications are Copyright (c) GSoft Group Inc., 2023.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

internal interface IClientCredentialsTokenEndpointService
{
    Task<ClientCredentialsToken> RequestTokenAsync(string clientName, CancellationToken cancellationToken);
}