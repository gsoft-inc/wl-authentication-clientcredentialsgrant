using CliWrap;

// TODO: Update just the output of copilot
public class OpenApiGenerationTests
{
    [Fact]
    public async Task TestGeneratedFileMatchesExpectedAsync()
    {
        var solutionPath = GetSolutionPath();
        
        // Define the paths
        var testsFolder = Path.Combine(solutionPath, "tests");
        var projectFolder = Path.Combine(testsFolder, "WebApi.OpenAPI.SystemTest");
        var generatedFilePath = Path.Combine(projectFolder, "openapi-v1.yaml");
        var expectedFilePath = Path.Combine(testsFolder, "expected-openapi-document.yaml");

        // Compile the project
        var result = await Cli.Wrap("dotnet")
            .WithWorkingDirectory(projectFolder)
            .WithValidation(CommandResultValidation.None)
            .WithArguments(a => a
                .Add("build"))
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
        
        var solutionDirectory = Directory.GetParent(assemblyDirectory).Parent.Parent.Parent.FullName;

        return solutionDirectory;
    }
}