using Microsoft.AspNetCore.Mvc;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace WebApi.OpenAPI.SystemTest.Controllers;

[ApiController]
public class ClientCredentialsController: ControllerBase
{
    [HttpGet]
    [Route("/controller-get")]
    [RequireClientCredentials("cocktail.drink")]
    public IActionResult DrinkCocktail(int id)
    {
        return this.Ok("Hello World!");
    }
    
    [HttpPost]
    [Route("/controller-post")]
    [RequireClientCredentials("cocktail.buy")]
    public IActionResult BuyCocktail(int id)
    {
        return this.Ok("Hello World!");
    }
}