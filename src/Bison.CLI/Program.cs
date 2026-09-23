using System.Net.Http.Json;
using System.CommandLine;
using System.CommandLine.Parsing;
using static UserInterface;
using SimpleDB;

string baseUrl =
    Environment.GetEnvironmentVariable("BISON_SERVER_URL")
    ?? "http://localhost:51234";

var client = new HttpClient();
client.BaseAddress = new Uri(baseUrl);

var taxonomy = TaxonomyLoader.Load();

var locationArgument = new Argument<string>("location");
var messageArgument = new Argument<string>("message");
var idArgument = new Argument<int>("id");
var taxonArgument = new Argument<string>("taxon");

var observeCommand = new Command("observe", "add a new observation");
observeCommand.Arguments.Add(messageArgument);
observeCommand.Arguments.Add(locationArgument);

var readCommand = new Command("read", "show all observations");

var commentCommand = new Command("comment", "add a comment to an observation");
commentCommand.Arguments.Add(idArgument);
commentCommand.Arguments.Add(messageArgument);

var discussionCommand = new Command("discussion", "show all comments for an observation");
discussionCommand.Arguments.Add(idArgument);

var proposeCommand = new Command("propose", "propose a taxon for an observation");
proposeCommand.Arguments.Add(idArgument);
proposeCommand.Arguments.Add(taxonArgument);

var proposalsCommand = new Command("proposals", "show all proposed taxons for an observation");
proposalsCommand.Arguments.Add(idArgument);

var locationCommand = new Command("location", "show all observations for a location");
locationCommand.Arguments.Add(locationArgument);

var rootCommand = new RootCommand();
rootCommand.Subcommands.Add(observeCommand);
rootCommand.Subcommands.Add(readCommand);
rootCommand.Subcommands.Add(commentCommand);
rootCommand.Subcommands.Add(discussionCommand);
rootCommand.Subcommands.Add(proposeCommand);
rootCommand.Subcommands.Add(proposalsCommand);
rootCommand.Subcommands.Add(locationCommand);

observeCommand.SetAction(async ParseResult =>

{
    string location = ParseResult.GetValue(locationArgument)!;
    string observation = ParseResult.GetValue(messageArgument)!;
    string author = Environment.UserName;
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    var observationRequest = new ObservationRequest(author, observation, timestamp, location);

    await client.PostAsJsonAsync("/observation", observationRequest);

    return 0;
});

readCommand.SetAction(async ParseResult =>
{
    var observations = await client.GetFromJsonAsync<List<ObservationRequest>>(
    $"/observations");

    PrintCheeps(observations!);

    return 0;
});

commentCommand.SetAction(async ParseResult =>
{
    int observationId = ParseResult.GetValue(idArgument);
    string message = ParseResult.GetValue(messageArgument)!;
    string author = Environment.UserName;
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var comment = new CommentRequest(author, message, timestamp, observationId);

    await client.PostAsJsonAsync("/comment", comment);

    return 0;
});

discussionCommand.SetAction(async ParseResult =>
{
    int observationId = ParseResult.GetValue(idArgument);
    var comments = await client.GetFromJsonAsync<List<CommentRequest>>(
    $"/comments?observationId={observationId}");

    PrintComments(comments!);
    
    return 0;
});

proposeCommand.SetAction(async ParseResult =>
{
    int observationId = ParseResult.GetValue(idArgument);
    string input = ParseResult.GetValue(taxonArgument)!;
    string author = Environment.UserName;
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    string? taxonId = taxonomy.GetById(input)?.TaxonId
        ?? taxonomy.GetByVernacularName(input)?.TaxonId;

    if (taxonId is null)
    {
        Console.WriteLine($"Unknown taxon: {input}");
        return 1;
    }

    var proposal = new ProposalRequest(author, taxonId, timestamp, observationId);
    var response = await client.PostAsJsonAsync("/proposal", proposal);

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine(await response.Content.ReadAsStringAsync());
        return 1;
    }

    return 0;
});

proposalsCommand.SetAction(async ParseResult =>
{
    int observationId = ParseResult.GetValue(idArgument);
    var proposals = await client.GetFromJsonAsync<List<ProposalRequest>>(
        $"/proposals?observationId={observationId}");

    PrintProposals(proposals!, taxonomy);

    return 0;
});

locationCommand.SetAction(async ParseResult =>
{
    string location = ParseResult.GetValue(locationArgument)!;
    var observations = await client.GetFromJsonAsync<List<ObservationRequest>>(
    $"/location?location={location}");

    PrintCheeps(observations!);

    return 0;
});

return rootCommand.Parse(args).Invoke();






