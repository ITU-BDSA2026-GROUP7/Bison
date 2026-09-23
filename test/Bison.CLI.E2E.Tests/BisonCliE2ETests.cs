using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;

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
        
        int port = GetFreePort();
        string url = $"http://localhost:{port}";

        process.StartInfo.EnvironmentVariables["BISON_SERVER_URL"] = url;

        // Act
        var server = RunServer(serverPath, tempDirectory, url);  
        await WaitForServerAsync(url);

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

        int port = GetFreePort();
        string url = $"http://localhost:{port}";

        process.StartInfo.EnvironmentVariables["BISON_SERVER_URL"] = url;

        // Act
        var server = RunServer(serverPath, tempDirectory, url);
        await WaitForServerAsync(url);

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

        int port = GetFreePort();
        string url = $"http://localhost:{port}";
       
        // Act
        var server = RunServer(serverPath, tempDirectory, url);
        await WaitForServerAsync(url);

        RunCli(projectPath, tempDirectory, "observe \"Penguin\" \"Antarctica\"", url);
        RunCli(projectPath, tempDirectory, "observe \"Puffin\" \"Iceland\"", url);

        Process process = RunCli(projectPath, tempDirectory, "location \"Antarctica\"", url);

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

    private static Process RunCli(string projectPath, string workingDirectory, string arguments, string url)
    {
        var process = new Process();

        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"run --project \"{projectPath}\" -- {arguments}";
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.EnvironmentVariables["BISON_SERVER_URL"] = url;

        process.Start();

        process.WaitForExit();

        return process;
    }
    private static Process RunServer(string projectPath, string workingDirectory, string url)
    {
        var process = new Process();

        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"run --project \"{projectPath}\" --no-launch-profile --urls {url}";
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.EnvironmentVariables["OBSERVE_FILE"] =
            Path.Combine(workingDirectory, "bison_observe_cli_db.csv");

        process.StartInfo.EnvironmentVariables["COMMENT_FILE"] =
            Path.Combine(workingDirectory, "bison_comment.csv");

        process.StartInfo.EnvironmentVariables["PROPOSAL_FILE"] =
            Path.Combine(workingDirectory, "bison_proposal.csv");

        process.Start();

        return process;
    }
    private static async Task WaitForServerAsync(string url) {
        using var client = new HttpClient();
        for (int i = 0; i < 30; i++)
        {
            try
            {
                var response =
                    await client.GetAsync(
                        url + "/observations");

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
    private static int GetFreePort() {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        int port =
            ((IPEndPoint)listener.LocalEndpoint).Port;

        listener.Stop();

        return port;
    }

    [Fact]
public async Task Fuzz_PostEndpoints()
{
    // Arrange
    string tempDirectory =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    Directory.CreateDirectory(tempDirectory);

    string serverPath =
        Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "src", "CSVDatabase.WebService"));

    int port = GetFreePort();
    string url = $"http://localhost:{port}";

    var server = RunServer(serverPath, tempDirectory, url);

    try
    {
        await WaitForServerAsync(url);

        var random = new Random(12345);

        var expectedObservations = new List<ObservationDto>();
        var expectedComments = new List<CommentDto>();
        var expectedProposals = new List<ProposalDto>();

        using var client = new HttpClient
        {
            BaseAddress = new Uri(url)
        };

        string taxonomyPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "src", "SimpleDB", "Taxons", "joined.csv"));

        var taxonIds =
            File.ReadLines(taxonomyPath)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(',')[0])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

        Assert.NotEmpty(taxonIds);

        // Generate observations
        for (int i = 0; i < 100; i++)
        {
            var observation = new
            {
                Author = $"User{random.Next(1, 10)}",
                Message = $"Observation {i}",
                Timestamp = random.NextInt64(1_700_000_000, 1_800_000_000),
                Location = $"Location{random.Next(1, 10)}"
            };

            expectedObservations.Add(
                new ObservationDto(
                    i + 1,
                    observation.Author,
                    observation.Message,
                    observation.Timestamp,
                    observation.Location));

            var response =
                await client.PostAsJsonAsync("/observation", observation);

            string responseBody =
                await response.Content.ReadAsStringAsync();

            Assert.True(
                response.IsSuccessStatusCode,
                $"Observation {i} failed: " +
                $"{(int)response.StatusCode} {response.StatusCode}.\n" +
                $"Response body: {responseBody}");
        }

        // Generate comments
        for (int i = 0; i < 100; i++)
        {
            bool useValidObservation = random.Next(100) < 90;

            int observationId = useValidObservation
                ? random.Next(1, expectedObservations.Count + 1)
                : expectedObservations.Count + random.Next(1, 100);

            var comment = new
            {
                Author = $"User{random.Next(1, 10)}",
                Message = $"Comment {i}",
                Timestamp = random.NextInt64(1_700_000_000, 1_800_000_000),
                ObservationId = observationId,
                Location = $"Location{random.Next(1, 10)}"
            };

            var response =
                await client.PostAsJsonAsync("/comment", comment);

            if (useValidObservation)
            {
                string responseBody =
                    await response.Content.ReadAsStringAsync();

                Assert.True(
                    response.IsSuccessStatusCode,
                    $"Comment {i} failed: " +
                    $"{(int)response.StatusCode} {response.StatusCode}.\n" +
                    $"Response body: {responseBody}");

                expectedComments.Add(
                    new CommentDto(
                        expectedComments.Count + 1,
                        comment.Author,
                        comment.Message,
                        comment.Timestamp,
                        comment.ObservationId,
                        comment.Location));
            }
            else
            {
                Assert.Equal(
                    HttpStatusCode.BadRequest,
                    response.StatusCode);
            }
        }

        // Generate proposals
        for (int i = 0; i < 100; i++)
        {
            bool useValidObservation = random.Next(100) < 90;
            bool useValidTaxon = random.Next(100) < 90;

            int observationId = useValidObservation
                ? random.Next(1, expectedObservations.Count + 1)
                : expectedObservations.Count + random.Next(1, 100);

            string taxonId = useValidTaxon
                ? taxonIds[random.Next(taxonIds.Count)]
                : $"invalid-{i}";

            var proposal = new
            {
                Author = $"User{random.Next(1, 10)}",
                TaxonId = taxonId,
                Timestamp = random.NextInt64(1_700_000_000, 1_800_000_000),
                ObservationId = observationId,
                Location = $"Location{random.Next(1, 10)}"
            };

            var response =
                await client.PostAsJsonAsync("/proposal", proposal);

            if (useValidObservation && useValidTaxon)
            {
                string responseBody =
                    await response.Content.ReadAsStringAsync();

                Assert.True(
                    response.IsSuccessStatusCode,
                    $"Proposal {i} failed: " +
                    $"{(int)response.StatusCode} {response.StatusCode}.\n" +
                    $"Response body: {responseBody}");

            expectedProposals.Add(
                new ProposalDto(
                    expectedProposals.Count + 1,
                    proposal.Author,
                    proposal.TaxonId,
                    proposal.Timestamp,
                    proposal.ObservationId,
                    proposal.Location));
        }
        else
        {
            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }
    }


        // Verify observations
        var actualObservations =
            await client.GetFromJsonAsync<List<ObservationDto>>(
                "/observations");

        Assert.NotNull(actualObservations);

        Assert.Equal(
            expectedObservations,
            actualObservations);

        // Verify comments
        foreach (var observation in expectedObservations)
        {
            var actualComments =
                await client.GetFromJsonAsync<List<CommentDto>>(
                    $"/comments?observationId={observation.Id}");

            Assert.NotNull(actualComments);

            var expectedForObservation =
                expectedComments
                    .Where(c => c.ObservationId == observation.Id)
                    .ToList();

            Assert.Equal(
                expectedForObservation,
                actualComments);
        }

        // Verify proposals
        foreach (var observation in expectedObservations)
        {
            var actualProposals =
                await client.GetFromJsonAsync<List<ProposalDto>>(
                    $"/proposals?observationId={observation.Id}");

            Assert.NotNull(actualProposals);

            var expectedForObservation =
                expectedProposals
                    .Where(p => p.ObservationId == observation.Id)
                    .ToList();

            Assert.Equal(
                expectedForObservation,
                actualProposals);
        }

        
    }
    finally
    {
        if (!server.HasExited)
        {
            server.Kill(entireProcessTree: true);
            await server.WaitForExitAsync();
        }

        Directory.Delete(tempDirectory, true);
    }
}

    private record ObservationDto(
    int Id,
    string Author,
    string Message,
    long Timestamp,
    string Location);

    private record CommentDto(
    int Id,
    string Author,
    string Message,
    long Timestamp,
    int ObservationId,
    string Location);

    private record ProposalDto(
    int Id,
    string Author,
    string TaxonId,
    long Timestamp,
    int ObservationId,
    string Location);

}