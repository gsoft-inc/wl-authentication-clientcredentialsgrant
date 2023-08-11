using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests;

public class JwtBearerOptionsTests
{
    [Theory]
    [MemberData(nameof(CreateTestCases))]
    public void Given_Faulty_JwtBearerOptions_When_GetOptions_Then_Throws(OptionsValidationTestData testData)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration, ConfigurationManager>();
        services.AddAuthentication()
            .AddClientCredentials(ClientCredentialsDefaults.AuthenticationScheme, testData.ConfigureOptions);

        using var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>();

        if (testData.IsErrorExpected)
        {
            var exception = Assert.Throws<OptionsValidationException>(() => optionsSnapshot.Get(ClientCredentialsDefaults.AuthenticationScheme));
            Assert.Equal(testData.ExpectedError, exception.Message);
        }
        else
        {
            _ = optionsSnapshot.Get(ClientCredentialsDefaults.AuthenticationScheme);
        }
    }

    [Theory]
    [InlineData("authority")]
    [InlineData("http://authority.io")]
    public void Given_JwtBearerOptions_With_Http_Authority_When_GetOptions_Then_Throws(string authority)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration, ConfigurationManager>();
        services.AddAuthentication()
            .AddClientCredentials(ClientCredentialsDefaults.AuthenticationScheme, opt =>
            {
                opt.Authority = authority;
            });

        using var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>();

        Assert.Throws<InvalidOperationException>(() => optionsSnapshot.Get(ClientCredentialsDefaults.AuthenticationScheme));
    }

    [Fact]
    public void Given_Other_JwtBearerOptions_Without_Authority_When_GetOptions_Then_No_Validation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddAuthentication().AddJwtBearer("OtherScheme", opt =>
        {
            opt.Authority = "https://authority.io";
        });

        using var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>();

        _ = optionsSnapshot.Get("OtherScheme");
    }

    public static IEnumerable<object[]> CreateTestCases()
    {
        yield return OptionsValidationTestData.ErrorTestCase("Authority '' should be in URI format; Audience cannot be empty", _ =>
        {
        });

        yield return OptionsValidationTestData.ErrorTestCase("Audience cannot be empty", options =>
        {
            options.Authority = "https://authority";
        });

        yield return OptionsValidationTestData.SuccessfulTestCase(options =>
        {
            options.Authority = "https://authority";
            options.Audience = "audience";
        });
    }

    public sealed class OptionsValidationTestData
    {
        private OptionsValidationTestData(string? expectedError, Action<JwtBearerOptions> configureOptions)
        {
            this.ExpectedError = expectedError;
            this.ConfigureOptions = configureOptions;
        }

        public string? ExpectedError { get; }

        public Action<JwtBearerOptions> ConfigureOptions { get; }

        public bool IsErrorExpected => this.ExpectedError != null;

        public static object[] ErrorTestCase(string expectedError, Action<JwtBearerOptions> configureOptions)
        {
            return new object[] { new OptionsValidationTestData(expectedError, configureOptions) };
        }

        public static object[] SuccessfulTestCase(Action<JwtBearerOptions> configureOptions)
        {
            return new object[] { new OptionsValidationTestData(null, configureOptions) };
        }

        public override string ToString()
        {
            return this.ExpectedError ?? "Successful test case";
        }
    }
}