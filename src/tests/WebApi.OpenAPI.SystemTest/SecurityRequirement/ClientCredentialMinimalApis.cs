using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace WebApi.OpenAPI.SystemTest.SecurityRequirement;

public static class ClientCredentialMinimalApis
{
    public static void AddMinimalEndpoint(this WebApplication app)
    {
        app.MapGet("minimal-api", () => "Hello World")
            .RequirePermission("cocktail.make")
            .WithTags("ClientCredentials")
            .WithOpenApi();
    }
}