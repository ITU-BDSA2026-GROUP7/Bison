using System.Diagnostics;

public class BisonCliE2ETests
{
    [Fact]
    public async Task Observe_StoresObservationInDatabase()
    {
        // Arrange
        string tempDirectory =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        string databasePath =
            Path.Combine(tempDirectory, "bison_observe_cli_db.csv");

        var process = new Process();

        process.StartInfo.FileName = "dotnet";

        string projectPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "src", "Bison.CLI"));

        process.StartInfo.Arguments =
            $"run --project \"{projectPath}\" -- observe \"Penguin\" \"Antarctica\"";
        
        process.StartInfo.WorkingDirectory = tempDirectory;

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;

        string serverPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "src", "CSVDatabase.WebService"));

        // Act
        var server = RunServer(serverPath, tempDirectory);  
        await WaitForServerAsync();

        process.Start();

        string processOutput = process.StandardOutput.ReadToEnd();
        string processError = process.StandardError.ReadToEnd();

        process.WaitForExit();

        Assert.True(process.ExitCode == 0,
            $"CLI failed.\nSTDOUT:\n{processOutput}\nSTDERR:\n{processError}");

        if (server.HasExited)
        {
            string serverError = server.StandardError.ReadToEnd();

            Assert.Fail(
                $"Server exited unexpectedly.\nSTDERR:\n{serverError}");
        }

        string databaseContents =
            File.ReadAllText(databasePath);

        Assert.Contains("Penguin", databaseContents);

        // Clean up
        server.Kill();
        server.WaitForExit();

        Directory.Delete(tempDirectory, true);
    }

    [Fact]
    public async Task Read_PrintsObservationsToConsole()
    {
        // Arrange
        string tempDirectory =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        string databasePath =
            Path.Combine(tempDirectory, "bison_observe_cli_db.csv");

        File.WriteAllText(
            databasePath,
            "Id,Author,Message,Timestamp,Location\r\n" +
            "1,Alice,Hello world,1725625800,SomeLocation\r\n");

        var process = new Process();

        process.StartInfo.FileName = "dotnet";

        string projectPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "src", "Bison.CLI"));

        process.StartInfo.Arguments =
            $"run --project \"{projectPath}\" -- read";

        process.StartInfo.WorkingDirectory = tempDirectory;

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;

        string serverPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "src", "CSVDatabase.WebService"));

        // Act
        var server = RunServer(serverPath, tempDirectory);
        await WaitForServerAsync();

        process.Start();

        string processOutput = process.StandardOutput.ReadToEnd();
        string processError = process.StandardError.ReadToEnd();

        process.WaitForExit();

        Assert.True(process.ExitCode == 0,
            $"CLI failed.\nSTDOUT:\n{processOutput}\nSTDERR:\n{processError}");

        if (server.HasExited)
        {
            string serverError = server.StandardError.ReadToEnd();

            Assert.Fail(
                $"Server exited unexpectedly.\nSTDERR:\n{serverError}");
        }

        Assert.Contains(
            "Alice @ 09/06/24 12:30:00: Hello world",
            processOutput);

        // Clean up
        server.Kill();
        server.WaitForExit();

        Directory.Delete(tempDirectory, true);
    }

    [Fact]
    public async Task Location_PrintsMatchingObservation()
    {
        // Arrange
        string tempDirectory =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        string databasePath =
            Path.Combine(tempDirectory, "bison_observe_cli_db.csv");

        string projectPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "src", "Bison.CLI"));

        string serverPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "src", "CSVDatabase.WebService"));

       
        // Act
        var server = RunServer(serverPath, tempDirectory);
        await WaitForServerAsync();

        RunCli(projectPath, tempDirectory, "observe \"Penguin\" \"Antarctica\"");
        RunCli(projectPath, tempDirectory, "observe \"Puffin\" \"Iceland\"");

        Process process = RunCli(projectPath, tempDirectory, "location \"Antarctica\"");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        Assert.True(process.ExitCode == 0,
            $"CLI failed.\nSTDOUT:\n{output}\nSTDERR:\n{error}");
            
        if (server.HasExited)
        {
            string serverError = server.StandardError.ReadToEnd();

            Assert.Fail(
                $"Server exited unexpectedly.\nSTDERR:\n{serverError}");
        }

        Assert.Contains("Penguin", output);
        Assert.DoesNotContain("Puffin", output);

        // Clean up
        server.Kill();
        server.WaitForExit();

        Directory.Delete(tempDirectory, true);
    }

    private static Process RunCli(string projectPath, string workingDirectory, string arguments)
    {
        var process = new Process();

        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"run --project \"{projectPath}\" -- {arguments}";
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;

        process.Start();

        process.WaitForExit();

        return process;
    }
    private static Process RunServer(string projectPath, string workingDirectory)
    {
        var process = new Process();

        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"run --project \"{projectPath}\" --urls http://localhost:51234";
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.EnvironmentVariables["OBSERVE_FILE"] = Path.Combine(workingDirectory, "bison_observe_cli_db.csv");
        process.StartInfo.EnvironmentVariables["COMMENT_FILE"] = Path.Combine(workingDirectory, "bison_comment.csv");

        process.Start();

        return process;
    }
    private static async Task WaitForServerAsync() {
        using var client = new HttpClient();
        for (int i = 0; i < 30; i++)
        {
            try
            {
                var response =
                    await client.GetAsync(
                        "http://localhost:51234/observations");

                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
            }

        await Task.Delay(1000);
        }
        throw new Exception("Server never became available.");
    }
}