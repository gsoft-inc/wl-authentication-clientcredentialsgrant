using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace WebApi.OpenAPI.SystemTest.SecurityRequirement;

public static class ClientCredentialMinimalApis
{
    public static void MapMinimalEndpoint(this WebApplication app)
    {
        app.MapGet("/minimal-api", () => "Hello World")
            .WithSummary("Given a minimal api RequirePermission with additional permissions, then should describe multiple permissions.")
            .RequireClientCredentials("cocktail.make", "cocktail.buy")
            .WithTags("ClientCredentials")
            .WithOpenApi();
    }
}