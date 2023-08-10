using System.Text.Json.Serialization;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class OpenIdConfiguration
{
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;
}