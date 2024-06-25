using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace WebApi.OpenAPI.SystemTest.SecurityRequirement;

[ApiController]
public class ClientCredentialsController : ControllerBase
{
    [HttpPost]
    [Route("weather")]
    [RequireClientCredentials("read")]
    public IActionResult SeeCocktail()
    {
        return this.Ok("Hello World!");
    }
}