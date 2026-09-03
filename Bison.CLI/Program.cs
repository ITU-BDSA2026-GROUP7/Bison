using SimpleDB;

const string filename = "bison_observe_cli_db.csv";

IDatabaseRepository<Cheep> database = new CSVDatabase<Cheep>(filename);

if (args.Length > 0 && args[0] == "observe")
{
    string observation = args[1];
    string author = Environment.UserName;
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    database.Store(new Cheep(author, observation, timestamp));
}
else
{
    foreach (Cheep cheep in database.Read())
    {
        DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeSeconds(cheep.Timestamp);

        string output =
            cheep.Author + " @ " +
            dateTime.ToString("MM/dd/yy HH':'mm':'ss") +
            ": " + cheep.Message;

        Console.WriteLine(output);
    }
}

public record Cheep(string Author, string Message, long Timestamp);
