using GSoft.AspNetCore.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant.Tests;

public class AuthenticationBuilderExtensionsTest
{
    [Fact]
    public async Task GivenAnAuthenticationBuilder_WhenConfigsArePresent_ThenOptionsAreSet()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { $"{ClientCredentialsDefaults.ClientCredentialsConfigSection}:Authority", "https://identity.local" },
            { $"{ClientCredentialsDefaults.ClientCredentialsConfigSection}:Audience", "audience" },
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
    public async Task GivenAnAuthenticationBuilder_WhenOptionsAreConfigured_ThenOptionsAreSet()
    {
        var services = new ServiceCollection();
        var authenticationBuilder = new AuthenticationBuilder(services);
        
        services.AddOptions<JwtBearerOptions>(ClientCredentialsDefaults.AuthenticationScheme)
            .Configure(options =>
            {
                options.Audience = "audience";
                options.Authority = "https://identity.local";
            });
        
        authenticationBuilder.AddClientCredentials(ClientCredentialsDefaults.AuthenticationScheme);

        var sp = services.BuildServiceProvider();
        
        var jwtBearerOptions = sp.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>().Get(ClientCredentialsDefaults.AuthenticationScheme);
        
        Assert.Equal("audience", jwtBearerOptions.Audience);
        Assert.Equal("https://identity.local", jwtBearerOptions.Authority);
    }    
    
    [Fact]
    public async Task GivenAnAuthenticationBuilder_WhenOptionsAreConfiguredWithAnAction_ThenOptionsAreSet()
    {
        var services = new ServiceCollection();
        var authenticationBuilder = new AuthenticationBuilder(services);
        
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
    public async Task GivenAnAuthenticationBuilder_WhenConfigsArePresent_ThenOptionsAreSet2()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        var authenticationBuilder = new AuthenticationBuilder(services);
        authenticationBuilder.AddClientCredentials();

        var sp = services.BuildServiceProvider();
        
        var jwtBearerOptions = sp.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>().Get(ClientCredentialsDefaults.AuthenticationScheme);
        
        Assert.Null( jwtBearerOptions.Audience);
        Assert.Null(jwtBearerOptions.Authority);
    }
    
    [Fact]
    public async Task GivenNoAuthenticationBuilder_WhenCalling_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AuthenticationBuilderExtensions.AddClientCredentials(null));
    }
    
    [Fact]
    public async Task GivenNoAuthenticationBuilder_WhenCallingWithScheme_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AuthenticationBuilderExtensions.AddClientCredentials(null, "SomeScheme", options => { }));
    }
}