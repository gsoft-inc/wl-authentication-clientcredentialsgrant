using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests;

public class AuthorizationExtensionsTest
{
    private const string DefaultAudience = "serviceA";
    private const string DefaultAuthority = "https://authority";

    [Fact]
    public void GivenNullIServiceCollection_WhenAddClientCredentialsAuthorization_ThenThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AuthorizationExtensions.AddClientCredentialsAuthorization(null!));
    }

    [Fact]
    public async Task GivenIServiceCollection_WhenAddClientCredentialsAuthorization_ThenPoliciesSet()
    {
        // Given
        var services = new ServiceCollection();

        services.AddOptions<JwtBearerOptions>(ClientCredentialsDefaults.AuthenticationScheme)
            .Configure(opt =>
            {
                opt.Audience = DefaultAudience;
                opt.Authority = DefaultAuthority;
            });

        // When
        services.AddClientCredentialsAuthorization();

        // Then
        await using var serviceProvider = services.BuildServiceProvider();

        var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>();
        var authorizationValues = authorizationOptions.Value;

        Assert.NotNull(authorizationValues);

        var readPolicy = authorizationValues.GetPolicy(ClientCredentialsDefaults.AuthorizationReadPolicy);
        ValidateClassicPolicy(readPolicy, ClientCredentialsScope.Read);

        var writePolicy = authorizationValues.GetPolicy(ClientCredentialsDefaults.AuthorizationWritePolicy);
        ValidateClassicPolicy(writePolicy, ClientCredentialsScope.Write);

        var adminPolicy = authorizationValues.GetPolicy(ClientCredentialsDefaults.AuthorizationAdminPolicy);
        ValidateClassicPolicy(adminPolicy, ClientCredentialsScope.Admin);
        
        var requirePermissionPolicy = authorizationValues.GetPolicy(ClientCredentialsDefaults.AuthorizationRequirePermissionsPolicy);
        ValidateRequirePermissionPolicy(requirePermissionPolicy);
    }
    
    [Fact]
    public async Task GivenIServiceCollection_WhenAddClientCredentialsAuthorization_ThenRequirementHandlerRegistered()
    {
        // Given
        var services = new ServiceCollection();

        // When
        services.AddClientCredentialsAuthorization();

        // Then
        await using var serviceProvider = services.BuildServiceProvider();

        var authorizationHandlers = serviceProvider.GetServices<IAuthorizationHandler>();
        Assert.Single(authorizationHandlers.OfType<RequireClientCredentialsRequirementHandler>());
    }

    private static void ValidateClassicPolicy(AuthorizationPolicy? policy, ClientCredentialsScope scope)
    {
        Assert.NotNull(policy);
        Assert.Collection(
            policy.AuthenticationSchemes,
            scheme =>
            {
                Assert.Equal(ClientCredentialsDefaults.AuthenticationScheme, scheme);
            });
        Assert.Equal(2, policy.Requirements.Count);
        Assert.Collection(
            policy.Requirements,
            x =>
            {
                Assert.Equal(typeof(DenyAnonymousAuthorizationRequirement), x.GetType());
            },
            x =>
            {
                Assert.Equal(typeof(ClaimsAuthorizationRequirement), x.GetType());
                var requirement = (ClaimsAuthorizationRequirement)x;

                Assert.Equal("scope", requirement.ClaimType);
                Assert.NotNull(requirement.AllowedValues);

                var allowedScope = Assert.Single(requirement.AllowedValues);
                Assert.Equal($"{DefaultAudience}:{AuthorizationExtensions.ScopeClaimMapping[scope]}", allowedScope);
            });
    }
    
    private static void ValidateRequirePermissionPolicy(AuthorizationPolicy? policy)
    {
        Assert.NotNull(policy);
        Assert.Collection(
            policy.AuthenticationSchemes,
            scheme =>
            {
                Assert.Equal(ClientCredentialsDefaults.AuthenticationScheme, scheme);
            });
        Assert.Collection(
            policy.Requirements,
            x =>
            {
                Assert.Equal(typeof(DenyAnonymousAuthorizationRequirement), x.GetType());
            },
            x =>
            {
                Assert.Equal(typeof(RequireClientCredentialsRequirement), x.GetType());
            });
    }
}