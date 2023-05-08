using GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GSoft.Authentication.ClientCredentialsGrant.Tests;

public class ClientCredentialsOptionsTests
{
    [Theory]
    [MemberData(nameof(CreateTestCases))]
    public void ClientCredentialsOptions_Validation_Works(OptionsValidationTestData testData)
    {
        var services = new ServiceCollection();
        services.AddHttpClient("Test").AddClientCredentialsHandler(testData.ConfigureOptions);

        using var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<ClientCredentialsOptions>>();

        if (testData.IsErrorExpected)
        {
            var exception = Assert.Throws<OptionsValidationException>(() => optionsSnapshot.Get("Test"));
            Assert.Equal(testData.ExpectedError, exception.Message);
        }
        else
        {
            _ = optionsSnapshot.Get("Test");
        }
    }

    public static IEnumerable<object[]> CreateTestCases()
    {
        yield return OptionsValidationTestData.ErrorTestCase("Authority '' should be in URI format; ClientId cannot be empty; ClientSecret cannot be empty", _ =>
        {
        });

        yield return OptionsValidationTestData.ErrorTestCase("Authority 'whatever' should be in URI format; ClientId cannot be empty; ClientSecret cannot be empty", options =>
        {
            options.Authority = "whatever";
        });

        yield return OptionsValidationTestData.ErrorTestCase("Authority 'http://whatever' should use the 'https' scheme; ClientId cannot be empty; ClientSecret cannot be empty", options =>
        {
            options.Authority = "http://whatever";
        });

        yield return OptionsValidationTestData.ErrorTestCase("ClientId cannot be empty; ClientSecret cannot be empty", options =>
        {
            options.Authority = "https://whatever";
        });

        yield return OptionsValidationTestData.ErrorTestCase("ClientSecret cannot be empty", options =>
        {
            options.Authority = "https://whatever";
            options.ClientId = "whatever";
        });

        yield return OptionsValidationTestData.ErrorTestCase("Scopes cannot be null", options =>
        {
            options.Authority = "https://whatever";
            options.ClientId = "whatever";
            options.ClientSecret = "whatever";
            options.Scopes = null!;
        });

        yield return OptionsValidationTestData.ErrorTestCase("CacheLifetimeBuffer must be greater than or equal to zero", options =>
        {
            options.Authority = "https://whatever";
            options.ClientId = "whatever";
            options.ClientSecret = "whatever";
            options.CacheLifetimeBuffer = TimeSpan.FromTicks(-1);
        });

        yield return OptionsValidationTestData.SuccessfulTestCase(options =>
        {
            options.Authority = "https://whatever";
            options.ClientId = "whatever";
            options.ClientSecret = "whatever";
        });
    }

    public sealed class OptionsValidationTestData
    {
        private OptionsValidationTestData(string? expectedError, Action<ClientCredentialsOptions> configureOptions)
        {
            this.ExpectedError = expectedError;
            this.ConfigureOptions = configureOptions;
        }

        public string? ExpectedError { get; }

        public Action<ClientCredentialsOptions> ConfigureOptions { get; }

        public bool IsErrorExpected => this.ExpectedError != null;

        public static object[] ErrorTestCase(string expectedError, Action<ClientCredentialsOptions> configureOptions)
        {
            return new object[] { new OptionsValidationTestData(expectedError, configureOptions) };
        }

        public static object[] SuccessfulTestCase(Action<ClientCredentialsOptions> configureOptions)
        {
            return new object[] { new OptionsValidationTestData(null, configureOptions) };
        }

        public override string ToString()
        {
            return this.ExpectedError ?? "Successful test case";
        }
    }
}