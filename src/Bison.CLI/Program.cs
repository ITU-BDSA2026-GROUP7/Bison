using SimpleDB;
using System.CommandLine;
using System.CommandLine.Parsing;
using static UserInterface;

const string observeFile = "bison_observe_cli_db.csv";
const string commentFile = "bison_comment.csv"; 

var locationArgument = new Argument<string>("location");
var messageArgument = new Argument<string>("message");
var idArgument = new Argument<int>("id");

var observeCommand = new Command("observe", "add a new observation");
observeCommand.Arguments.Add(messageArgument);
observeCommand.Arguments.Add(locationArgument);

var readCommand = new Command("read", "show all observations");

var commentCommand = new Command("comment", "add a comment to an observation");
commentCommand.Arguments.Add(idArgument);
commentCommand.Arguments.Add(messageArgument);

var discussionCommand = new Command("discussion", "show all comments for an observation");
discussionCommand.Arguments.Add(idArgument);

var locationCommand = new Command("location", "show all observations for a location");
locationCommand.Arguments.Add(locationArgument);

var rootCommand = new RootCommand();
rootCommand.Subcommands.Add(observeCommand);
rootCommand.Subcommands.Add(readCommand);
rootCommand.Subcommands.Add(commentCommand);
rootCommand.Subcommands.Add(discussionCommand);
rootCommand.Subcommands.Add(locationCommand);

var taxonomy = TaxonomyLoader.Load();
Console.WriteLine("Taxonomy loaded");

var fishHeron = taxonomy.GetByVernacularName("Fiskehejre");
if (fishHeron != null){
    Console.WriteLine($"Found: {fishHeron.ScientificName}");
} else {
    Console.WriteLine("Taxon not found");
}

var taxon = taxonomy.GetById("MSTSNM:Arter:c28811f4-f785-ea11-aa77-501ac539d1ea");
Console.WriteLine(taxon?.ScientificName);

var parent = taxonomy.GetParent("MSTSNM:Arter:c28811f4-f785-ea11-aa77-501ac539d1ea");
Console.WriteLine(parent?.ScientificName);

var children = taxonomy.GetChildren("MSTSNM:Arter:7f9ef9f3-f785-ea11-aa77-501ac539d1ea");
foreach(var child in children)
{
    Console.WriteLine(child.ScientificName);
}

IDatabaseRepository<Cheep> database = CSVDatabase<Cheep>.Instance(observeFile);
IDatabaseRepository<Comment> commentDatabase = CSVDatabase<Comment>.Instance(commentFile);

var observationService = new ObservationService(database);



Console.WriteLine(fishHeron == null ? "Fiskehjere not found" : fishHeron.ScientificName);
Console.WriteLine($"Loaded {taxonomy.Count()} taxons");


Console.WriteLine(fishHeron?.ScientificName);

observeCommand.SetAction(ParseResult =>
{
    string location = ParseResult.GetValue(locationArgument)!;
    string observation = ParseResult.GetValue(messageArgument)!;
    string author = Environment.UserName;
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    observationService.AddObservation(
        observation,
        author,
        timestamp,
        location);

    return 0;
});

readCommand.SetAction(ParseResult =>
{
    UserInterface.PrintCheeps(database.Read());
    return 0;
});

commentCommand.SetAction(ParseResult =>
{
    int observationId = ParseResult.GetValue(idArgument);
    string message = ParseResult.GetValue(messageArgument)!;

    bool observationExists = database.Read().Any(o => o.Id == observationId);
    if (!observationExists)
    {
        return 0; // silently dropped if the observation id doesn't exist
    }

    string author = Environment.UserName;
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    var existingComments = commentDatabase.Read();
    int nextCommentId = existingComments.Any() ? existingComments.Max(c => c.Id) + 1 : 1;

    commentDatabase.Store(new Comment(nextCommentId, author, message, timestamp, observationId, string.Empty));
    return 0;
});

discussionCommand.SetAction(ParseResult =>
{
    int observationId = ParseResult.GetValue(idArgument);
    var comments = commentDatabase.Read().Where(c => c.ObservationId == observationId);
    UserInterface.PrintCheeps(comments);
    return 0;
});

locationCommand.SetAction(ParseResult =>
{
    string location = ParseResult.GetValue(locationArgument)!;
    UserInterface.PrintCheeps(observationService.GetObservationsByLocation(location));
    return 0;
});

return rootCommand.Parse(args).Invoke();






