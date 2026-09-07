using SimpleDB;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using static UserInterface;

const string observeFile = "bison_observe_cli_db.csv";
const string commentFile = "bison.comment.csv";

var messageArgument = new Argument<string>("message");
var idArgument = new Argument<int>("id");

var observeCommand = new Command("observe", "add a new observation");
observeCommand.Arguments.Add(messageArgument);

var readCommand = new Command("read", "show all obervation");

var commentCommand = new Command("comment", "add a comment to an observation");
commentCommand.Argument.Add(messageArgument);
commentCommand.Argument.Add(idArgument);

var rootCommand = new RootCommand();
rootCommand.Subcommands.Add(observeCommand);
rootCommand.Subcommands.Add(readCommand);
rootCommand.Subcommands.Add(commentCommand);

IDatabaseRepository<Cheep> database = new CSVDatabase<Cheep>(observeFile);
IDatabaseRepository<Comment> commentDatabase = new CSVDatabase<Comment>(commentFile);

observeCommand.SetAction(ParseResult =>
{
    string observation = ParseResult.GetValue(messageArgument)!;
    string author = Environment.UserName;
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    database.Store(new Cheep(author, observation, timestamp));
    return 0;
});

readCommand.SetAction(ParseResult =>
{   
    UserInterface.PrintCheeps(database.Read());
    return 0;
});

return rootCommand.Parse(args).Invoke();

public record Cheep(string Author, string Message, long Timestamp);

public record Comment(string abdallah);
