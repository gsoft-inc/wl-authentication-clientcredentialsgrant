using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant.Tests;

public class AuthenticationBuilderExtensionsTests
{
    [Fact]
    public void GivenAnAuthenticationBuilder_WhenConfigsArePresent_ThenOptionsAreSet()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { $"Authentication:Schemes:{ClientCredentialsDefaults.AuthenticationScheme}:Authority", "https://identity.local" },
            { $"Authentication:Schemes:{ClientCredentialsDefaults.AuthenticationScheme}:Audience", "audience" },
        };
        
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        var authenticationBuilder = new AuthenticationBuilder(services);
        
        authenticationBuilder.AddClientCredentials();

        var sp = services.BuildServiceProvider();
        
        var jwtBearerOptions = sp.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>().Get(ClientCredentialsDefaults.AuthenticationScheme);
        
        Assert.Equal("audience", jwtBearerOptions.Audience);
        Assert.Equal("https://identity.local", jwtBearerOptions.Authority);
    }
    
    [Fact]
    public void GivenAnAuthenticationBuilder_WhenOptionsAreConfigured_ThenOptionsAreSet()
    {
        var services = new ServiceCollection();
        var authenticationBuilder = new AuthenticationBuilder(services);
        
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddOptions<JwtBearerOptions>(ClientCredentialsDefaults.AuthenticationScheme)
            .Configure(options =>
            {
                options.Audience = "audience";
                options.Authority = "https://identity.local";
            });
        
        authenticationBuilder.AddClientCredentials();

        var sp = services.BuildServiceProvider();
        
        var jwtBearerOptions = sp.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>().Get(ClientCredentialsDefaults.AuthenticationScheme);
        
        Assert.Equal("audience", jwtBearerOptions.Audience);
        Assert.Equal("https://identity.local", jwtBearerOptions.Authority);
    }    
    
    [Fact]
    public void GivenAnAuthenticationBuilder_WhenOptionsAreConfiguredWithAnAction_ThenOptionsAreSet()
    {
        var services = new ServiceCollection();
        var authenticationBuilder = new AuthenticationBuilder(services);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        
        authenticationBuilder.AddClientCredentials(ClientCredentialsDefaults.AuthenticationScheme, options =>
        {
            options.Audience = "audience";
            options.Authority = "https://identity.local";
        });

        var sp = services.BuildServiceProvider();
        
        var jwtBearerOptions = sp.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>().Get(ClientCredentialsDefaults.AuthenticationScheme);
        
        Assert.Equal("audience", jwtBearerOptions.Audience);
        Assert.Equal("https://identity.local", jwtBearerOptions.Authority);
    }    
    
    [Fact]
    public void GivenAnAuthenticationBuilder_WhenConfigsArePresent_ThenOptionsAreSet2()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        var authenticationBuilder = new AuthenticationBuilder(services);
        authenticationBuilder.AddClientCredentials();

        var sp = services.BuildServiceProvider();
        
        var jwtBearerOptions = sp.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>().Get(ClientCredentialsDefaults.AuthenticationScheme);
        
        Assert.Null(jwtBearerOptions.Audience);
        Assert.Null(jwtBearerOptions.Authority);
    }
    
    [Fact]
    public void GivenNoAuthenticationBuilder_WhenCalling_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AuthenticationBuilderExtensions.AddClientCredentials(null!));
    }
}