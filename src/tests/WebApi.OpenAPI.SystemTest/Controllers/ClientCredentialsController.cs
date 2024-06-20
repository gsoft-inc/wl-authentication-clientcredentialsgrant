using Microsoft.AspNetCore.Mvc;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace WebApi.OpenAPI.SystemTest.Controllers;

[ApiController]
public class ClientCredentialsController: ControllerBase
{
    [HttpGet]
    [Route("/example")]
    [RequireClientCredentials("cocktail:drink")]
    public IActionResult GetExample(int id)
    {
        return this.Ok("Hello World!");
    }
}