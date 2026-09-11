using SimpleDB;
using System.CommandLine;
using System.CommandLine.Parsing;
using static UserInterface;

const string observeFile = "bison_observe_cli_db.csv";
const string commentFile = "bison_comment.csv"; 

var messageArgument = new Argument<string>("message");
var idArgument = new Argument<int>("id");

var observeCommand = new Command("observe", "add a new observation");
observeCommand.Arguments.Add(messageArgument);

var readCommand = new Command("read", "show all observations");

var commentCommand = new Command("comment", "add a comment to an observation");
commentCommand.Arguments.Add(idArgument);
commentCommand.Arguments.Add(messageArgument);

var discussionCommand = new Command("discussion", "show all comments for an observation");
discussionCommand.Arguments.Add(idArgument);

var rootCommand = new RootCommand();
rootCommand.Subcommands.Add(observeCommand);
rootCommand.Subcommands.Add(readCommand);
rootCommand.Subcommands.Add(commentCommand);
rootCommand.Subcommands.Add(discussionCommand);

IDatabaseRepository<Cheep> database = CSVDatabase<Cheep>.Instance(observeFile);
IDatabaseRepository<Comment> commentDatabase = CSVDatabase<Comment>.Instance(commentFile);

var observationService = new ObservationService(database);

observeCommand.SetAction(ParseResult =>
{
    string observation = ParseResult.GetValue(messageArgument)!;
    string author = Environment.UserName;
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    observationService.AddObservation(
        observation,
        author,
        timestamp);

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

    commentDatabase.Store(new Comment(nextCommentId, author, message, timestamp, observationId));
    return 0;
});

discussionCommand.SetAction(ParseResult =>
{
    int observationId = ParseResult.GetValue(idArgument);
    var comments = commentDatabase.Read().Where(c => c.ObservationId == observationId);
    UserInterface.PrintCheeps(comments);
    return 0;
});

return rootCommand.Parse(args).Invoke();

public record Cheep(int Id, string Author, string Message, long Timestamp);

public record Comment(int Id, string Author, string Message, long Timestamp, int ObservationId) : Cheep(Id, Author, Message, Timestamp);