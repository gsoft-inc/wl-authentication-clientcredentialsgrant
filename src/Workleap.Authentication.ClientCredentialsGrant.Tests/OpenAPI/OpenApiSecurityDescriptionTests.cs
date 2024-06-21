using CliWrap;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests.OpenAPI;

public class OpenApiSecurityDescriptionTests
{
    [Fact(Skip = "Test if fix build")]
    public async Task Given_API_With_Client_Credential_Attribute_When_Generating_OpenAPI_Then_Equal_Expected_Document()
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
            .WithArguments(a => a
                .Add("build")
                .Add("--no-incremental"))
            .ExecuteAsync();

        // Check if the build was successful
        Assert.Equal(0, result.ExitCode);

        // Compare the generated file with the expected file
        var expectedFileContent = await File.ReadAllTextAsync(expectedFilePath);
        var generatedFileContent = await File.ReadAllTextAsync(generatedFilePath);

        Assert.Equal(expectedFileContent, generatedFileContent);
    }
    
    // TODO: Make this pretty
    private static string GetSolutionPath()
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        
        var solutionDirectory = Directory.GetParent(assemblyDirectory!)!.Parent!.Parent!.Parent!.FullName;

        return solutionDirectory;
    }
}