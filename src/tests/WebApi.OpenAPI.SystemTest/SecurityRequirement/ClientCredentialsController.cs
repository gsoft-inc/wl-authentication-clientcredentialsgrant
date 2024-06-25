using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace WebApi.OpenAPI.SystemTest.SecurityRequirement;

[ApiController]
[RequireClientCredentials("cocktail.drink")]
public class ClientCredentialsController : ControllerBase
{
    [HttpGet]
    [Route("/controller-allow-anonymous")]
    [SwaggerOperation(Summary = "See menu")]
    public IActionResult SeeMenu()
    {
        return this.Ok("Hello World!");
    }
    
    [HttpGet]
    [Route("/controller-using-attribute-at-class-level")]
    [SwaggerOperation(Summary = "Drink a cocktail")]
    public IActionResult DrinkCocktail(int id)
    {
        return this.Ok("Hello World!");
    }
    
    [HttpPost]
    [Route("/controller-using-attribute-at-method-level")]
    [RequireClientCredentials("cocktail.buy", "cocktail.drink")]
    public IActionResult BuyCocktail(int id)
    {
        return this.Ok("Hello World!");
    }
}