using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace WebApi.OpenAPI.SystemTest.SecurityRequirement;

[ApiController]
public class ClientCredentialsController : ControllerBase
{
    [HttpGet]
    [Route("/controller-single-permission-with-summary")]
    [SwaggerOperation(Summary = "Drink a cocktail")]
    [RequireClientCredentials("cocktail.drink")]
    public IActionResult DrinkCocktail(int id)
    {
        return this.Ok("Hello World!");
    }
    
    [HttpPost]
    [Route("/controller-multiple-permissions")]
    [RequireClientCredentials("cocktail.buy", "cocktail.drink")]
    public IActionResult BuyCocktail(int id)
    {
        return this.Ok("Hello World!");
    }
}