using CliWrap;
using CliWrap.Buffered;
using Meziantou.Framework;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests.OpenAPI;

public class OpenApiSecurityDescriptionTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Given_API_With_Client_Credentials_Attribute_When_Generating_OpenAPI_Then_Equal_Expected_Document()
    {
        var solutionPath = GetSolutionPath();

        var testsFolder = Path.Combine(solutionPath, "tests");
        var projectFolder = Path.Combine(testsFolder, "WebApi.OpenAPI.SystemTest");
        var generatedFilePath = Path.Combine(projectFolder, "openapi-v1.yaml");
        var expectedFilePath = Path.Combine(testsFolder, "expected-openapi-document.yaml");

        // Compile the project
        var result = await Cli.Wrap("dotnet")
            .WithWorkingDirectory(projectFolder)
            .WithValidation(CommandResultValidation.None)
            .WithArguments(["build", "--no-incremental"])
            .ExecuteBufferedAsync();

        testOutputHelper.WriteLine("Build output:");
        testOutputHelper.WriteLine(result.StandardError);
        testOutputHelper.WriteLine(result.StandardOutput);

        // Check if the build was successful
        Assert.Equal(0, result.ExitCode);

        // Compare the generated file with the expected file
        var expectedFileContent = await File.ReadAllTextAsync(expectedFilePath);
        var generatedFileContent = await File.ReadAllTextAsync(generatedFilePath);

        Assert.Equal(expectedFileContent, generatedFileContent, ignoreLineEndingDifferences: true);
    }

    private static string GetSolutionPath()
    {
        return GetGitRoot() / "src";
    }

    private static FullPath GetGitRoot()
    {
        if (FullPath.CurrentDirectory().TryFindFirstAncestorOrSelf(current => Directory.Exists(current / ".git"), out var root))
        {
            return root;
        }

        throw new InvalidOperationException("git root folder not found");
    }
}