using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace WebApi.OpenAPI.SystemTest.SecurityRequirement;

[ApiController]
public class ClientCredentialsController : ControllerBase
{
    [HttpPost]
    [Route("/controller-allow-anonymous-override")]
    [SwaggerOperation(Summary = "Given endpoint with [RequireClientCredentials] and [AllowAnonymous], then OpenAPI should contains no permission.")]
    [RequireClientCredentials("cocktail.drink")]
    [AllowAnonymous]
    public IActionResult SeeCocktail(int id)
    {
        return this.Ok("Hello World!");
    }
    
    [HttpPost]
    [Route("/controller-multiple-permissions")]
    [SwaggerOperation(Summary = "Given endpoint with [RequireClientCredentials], then OpenAPI should contains permission.")]
    [RequireClientCredentials("cocktail.buy", "cocktail.drink")]
    public IActionResult BuyCocktail(int id)
    {
        return this.Ok("Hello World!");
    }
}