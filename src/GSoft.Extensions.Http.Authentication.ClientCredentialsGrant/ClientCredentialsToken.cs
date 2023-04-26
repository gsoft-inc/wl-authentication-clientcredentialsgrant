// This file is based on https://github.com/DuendeSoftware/Duende.AccessTokenManagement/blob/1.1.0/src/Duende.AccessTokenManagement/ClientCredentialsToken.cs
// Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0. All rights reserved.
//
// The original file has been significantly modified, and these modifications are Copyright (c) GSoft Group Inc., 2023.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class ClientCredentialsToken
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("expiration")]
    public DateTimeOffset Expiration { get; init; }

    private bool Equals(ClientCredentialsToken other)
    {
        return this.AccessToken == other.AccessToken && this.Expiration.Equals(other.Expiration);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is ClientCredentialsToken other && this.Equals(other));
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (this.AccessToken.GetHashCode() * 397) ^ this.Expiration.GetHashCode();
        }
    }
}