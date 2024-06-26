﻿using System.Security.Claims;
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
        
        var requiredPermissions = new List<string>
        {
            "requiredPermission",
        };

        var context = this.ConfigureHandlerContext(userClaims, requiredPermissions);
        var handler = this.ConfigureHandler(new JwtBearerOptions());

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
        
        var requiredPermissions = new List<string>
        {
            "requiredPermission",
            "alternativeRequiredPermission",
        };

        var context = this.ConfigureHandlerContext(userClaims, requiredPermissions);
        var handler = this.ConfigureHandler(new JwtBearerOptions()
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
        
        var requiredPermissions = new List<string>
        {
            "randomPermission",
        };

        var context = this.ConfigureHandlerContext(userClaims, requiredPermissions);
        var handler = this.ConfigureHandler(new JwtBearerOptions());

        // When
        await handler.HandleAsync(context);

        // Then
        Assert.False(context.HasSucceeded);
    }
    
    private RequireClientCredentialsRequirementHandler ConfigureHandler(JwtBearerOptions jwtOptions)
    {
        var jwtOptionsMonitor = A.Fake<IOptionsMonitor<JwtBearerOptions>>();
        A.CallTo(() => jwtOptionsMonitor.Get(ClientCredentialsDefaults.AuthenticationScheme)).Returns(jwtOptions);
        
        return new RequireClientCredentialsRequirementHandler(jwtOptionsMonitor);
    }

    private AuthorizationHandlerContext ConfigureHandlerContext(List<Claim> claims, List<string> requiredPermissions)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var requiredPermissionsAttribute = new RequireClientCredentialsAttribute(requiredPermissions.First(), requiredPermissions.Skip(1).ToArray());

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IEndpointFeature>(new EndpointFeature
        {
            Endpoint = new Endpoint(default, new EndpointMetadataCollection(requiredPermissionsAttribute), default),
        });

        return new AuthorizationHandlerContext(new[] { new RequireClientCredentialsRequirement() }, user, httpContext);
    }
    
    private sealed class EndpointFeature : IEndpointFeature
    {
        public Endpoint? Endpoint { get; set; }
    }
}