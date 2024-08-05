using System.Security.Claims;
using FakeItEasy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests;

public class RequireClientCredentialsRequirementHandlerTests
{
    [Theory]
    [InlineData("scope")]
    [InlineData("scp")]
    [InlineData("http://schemas.microsoft.com/identity/claims/scope")]
    public async Task GivenUserHaveTheRequiredScopesInOneOfClaimType_WhenHandleRequirement_ThenSucceeded(string scopeClaimType)
    {
        // Given
        var userClaims = new List<Claim>
        {
            new(scopeClaimType, "requiredPermission"),
            new(scopeClaimType, "otherPermission"),
        };

        var requiredPermission = "requiredPermission";

        var context = ConfigureHandlerContext(userClaims, requiredPermission);
        var handler = ConfigureHandler(new JwtBearerOptions());

        // When
        await handler.HandleAsync(context);

        // Then
        Assert.True(context.HasSucceeded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenUserHaveOneOfTheRequiredScopes_WhenHandleRequirement_ThenSucceeded(bool usePrefixAudienceFormat)
    {
        // Given
        var expectedAudience = "invoices";

        var userClaims = new List<Claim>
        {
            new("scope", usePrefixAudienceFormat ? $"{expectedAudience}:requiredPermission" : "requiredPermission"),
            new("scope", "otherPermission"),
        };

        var requiredPermission = "requiredPermission";

        var context = ConfigureHandlerContext(userClaims, requiredPermission);
        var handler = ConfigureHandler(new JwtBearerOptions
        {
            Audience = expectedAudience,
        });

        // When
        await handler.HandleAsync(context);

        // Then
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task GivenUserDoNotHaveTheRequiredScopes_WhenHandleRequirement_ThenNotSucceeded()
    {
        // Given
        var userClaims = new List<Claim>
        {
            new("scope", "requiredPermission1"),
            new("scope", "otherPermission"),
        };

        var context = ConfigureHandlerContext(userClaims, "randomPermission");
        var handler = ConfigureHandler(new JwtBearerOptions());

        // When
        await handler.HandleAsync(context);

        // Then
        Assert.False(context.HasSucceeded);
    }

    private static RequireClientCredentialsRequirementHandler ConfigureHandler(JwtBearerOptions jwtOptions)
    {
        var jwtOptionsMonitor = A.Fake<IOptionsMonitor<JwtBearerOptions>>();
        A.CallTo(() => jwtOptionsMonitor.Get(ClientCredentialsDefaults.AuthenticationScheme)).Returns(jwtOptions);

        return new RequireClientCredentialsRequirementHandler(jwtOptionsMonitor);
    }

    private static AuthorizationHandlerContext ConfigureHandlerContext(List<Claim> claims, string requiredPermission)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var requireClientCredentialsAttribute = new RequireClientCredentialsAttribute(requiredPermission);

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IEndpointFeature>(new EndpointFeature
        {
            Endpoint = new Endpoint(requestDelegate: default, new EndpointMetadataCollection(requireClientCredentialsAttribute), displayName: default),
        });

        return new AuthorizationHandlerContext(new[] { new RequireClientCredentialsRequirement() }, user, httpContext);
    }

    private sealed class EndpointFeature : IEndpointFeature
    {
        public Endpoint? Endpoint { get; set; }
    }
}