using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace WebApi.OpenAPI.SystemTest.Endpoints;

[ApiController]
public class ClientCredentialsController : ControllerBase
{
    [HttpPost]
    [Route("/controller-allow-anonymous-override")]
    [SwaggerOperation(Summary = "This controller method decorated with both AllowAnonymous and RequireClientCredentials should not require any permissions.")]
    [RequireClientCredentials("cocktail.drink")]
    [AllowAnonymous]
    public IActionResult SeeCocktail(int id)
    {
        return this.Ok("Hello World!");
    }
    
    [HttpPost]
    [Route("/controller-requires-permission")]
    [SwaggerOperation(Summary = "This controller method should require the cocktail.buy permission.")]
    [RequireClientCredentials("cocktail.buy")]
    public IActionResult BuyCocktail(int id)
    {
        return this.Ok("Hello World!");
    }
}