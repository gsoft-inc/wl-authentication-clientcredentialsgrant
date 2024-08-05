using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace WebApi.OpenAPI.SystemTest.Endpoints;

public static class ClientCredentialMinimalApis
{
    public static void MapMinimalEndpoint(this WebApplication app)
    {
        app.MapGet("/minimal-api", () => "Hello World")
            .WithSummary("This minimal API should require the cocktail.make permission.")
            .RequireClientCredentials("cocktail.make")
            .WithTags("ClientCredentials")
            .WithOpenApi();
    }
}