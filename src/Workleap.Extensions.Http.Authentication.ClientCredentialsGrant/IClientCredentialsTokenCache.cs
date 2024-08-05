// This file is based on https://github.com/DuendeSoftware/Duende.AccessTokenManagement/blob/1.1.0/src/Duende.AccessTokenManagement/Interfaces/IClientCredentialsTokenCache.cs
// Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0. All rights reserved.
//
// The original file has been significantly modified, and these modifications are Copyright (c) Workleap, 2023.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal interface IClientCredentialsTokenCache
{
    Task<DateTimeOffset> SetAsync(string clientName, ClientCredentialsToken token, CancellationToken cancellationToken);

    Task<ClientCredentialsToken?> GetAsync(string clientName, CancellationToken cancellationToken);
}