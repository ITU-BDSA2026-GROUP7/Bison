using System.Diagnostics;

public class BisonCliE2ETests
{
    [Fact]
    public void Observe_StoresObservationInDatabase()
    {
        // Arrange
        string tempDirectory =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

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

        Thread.Sleep(3000);

        process.Start();
        
        process.WaitForExit();  

        server.Kill();
        server.WaitForExit();

        // Assert
        string databasePath =
            Path.Combine(tempDirectory, "bison_observe_cli_db.csv");

        string databaseContents =
            File.ReadAllText(databasePath);

    
        Assert.Contains("Penguin", databaseContents);

        // Clean up
        Directory.Delete(tempDirectory, true);
    }

    [Fact]
    public void Read_PrintsObservationsToConsole()
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

        Thread.Sleep(3000);

        process.Start();

        string output = process.StandardOutput.ReadToEnd();

        process.WaitForExit();

        server.Kill();
        server.WaitForExit();

        // Assert
        Assert.Contains(
            "Alice @ 09/06/24 12:30:00: Hello world",
            output);

        // Clean up
        Directory.Delete(tempDirectory, true);
    }

    [Fact]
    public void Location_PrintsMatchingObservation()
    {
        // Arrange
        string tempDirectory =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

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

        Thread.Sleep(3000);

        RunCli(projectPath, tempDirectory, "observe \"Penguin\" \"Antarctica\"");
        RunCli(projectPath, tempDirectory, "observe \"Puffin\" \"Iceland\"");

        string output = RunCli(projectPath, tempDirectory, "location \"Antarctica\"");

        // Assert
        Assert.Contains("Penguin", output);
        Assert.DoesNotContain("Puffin", output);

        // Clean up
        server.Kill();
        server.WaitForExit();

        Directory.Delete(tempDirectory, true);
    }

    private static string RunCli(string projectPath, string workingDirectory, string arguments)
    {
        var process = new Process();

        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"run --project \"{projectPath}\" -- {arguments}";
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;

        process.Start();

        string output = process.StandardOutput.ReadToEnd();

        process.WaitForExit();

        return output;
    }
    private static Process RunServer(string projectPath, string workingDirectory)
    {
        var process = new Process();

        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"run --project \"{projectPath}\" urls -- http://localhost:5000";
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;

        process.Start();

        return process;
    }
}