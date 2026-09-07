using SimpleDB;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using static UserInterface;

const string filename = "bison_observe_cli_db.csv";



var messageArgument = new Argument<string>("message");

var observeCommand = new Command("observe", "add a new observation");
observeCommand.Arguments.Add(messageArgument);

var readCommand = new Command("read", "show all obervation");

var rootCommand = new RootCommand();
rootCommand.Subcommands.Add(observeCommand);
rootCommand.Subcommands.Add(readCommand);

IDatabaseRepository<Cheep> database = new CSVDatabase<Cheep>(filename);

observeCommand.SetAction(ParseResult =>
{
    string observation = ParseResult.GetValue(messageArgument)!;
    string author = Environment.UserName;
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    var existing = database.Read();
    int nextId = existing.Any() ? existing.Max(c => c.Id) + 1 : 1;

    database.Store(new Cheep(nextId, author, observation, timestamp));
    return 0;
});

readCommand.SetAction(ParseResult =>
{
    /*
    foreach(Cheep cheep in database.Read())
    {
        DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeSeconds(cheep.Timestamp);
        string output = cheep.Author + " @ " + dateTime.ToString("MM/dd/yy HH':'mm':'ss") + ": " + cheep.Message;
        Console.WriteLine(output);
    }
    */
    
    UserInterface.PrintCheeps(database.Read());
    return 0;
});

return rootCommand.Parse(args).Invoke();

public record Cheep(int Id, string Author, string Message, long Timestamp);
