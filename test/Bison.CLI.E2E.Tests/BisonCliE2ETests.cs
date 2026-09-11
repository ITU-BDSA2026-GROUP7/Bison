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
            $"run --project \"{projectPath}\" -- observe \"Penguin\"";
        
        process.StartInfo.WorkingDirectory = tempDirectory;

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;  

        // Act
        process.Start();
        
        process.WaitForExit();  

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
            "Id,Author,Message,Timestamp\r\n" +
            "1,Alice,Hello world,1725625800\r\n");

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

        // Act
        process.Start();

        string output = process.StandardOutput.ReadToEnd();

        process.WaitForExit();

        // Assert
        Assert.Contains(
            "Alice @ 09/06/24 12:30:00: Hello world",
            output);

        // Clean up
        Directory.Delete(tempDirectory, true);
    }
}