// This file is based on https://github.com/DuendeSoftware/Duende.AccessTokenManagement/blob/1.1.0/src/Duende.AccessTokenManagement/ClientCredentialsToken.cs
// Copyright (c) Brock Allen & Dominick Baier, licensed under the Apache License, Version 2.0. All rights reserved.
//
// The original file has been significantly modified, and these modifications are Copyright (c) Workleap, 2023.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class ClientCredentialsToken
{
    public ClientCredentialsToken()
    {
    }

    public ClientCredentialsToken(string accessToken, DateTimeOffset expiration)
    {
        this.AccessToken = accessToken;
        this.Expiration = expiration;
    }

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("expiration")]
    public DateTimeOffset Expiration { get; init; }

    private bool Equals(ClientCredentialsToken other)
    {
        return this.AccessToken == other.AccessToken && this.Expiration.Equals(other.Expiration);
    }

    public TimeSpan GetTimeToLive(DateTimeOffset now)
    {
        var timeToLive = this.Expiration - now;
        return timeToLive > TimeSpan.Zero ? timeToLive : TimeSpan.Zero;
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