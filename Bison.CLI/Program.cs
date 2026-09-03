// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

String filename = "bison_observe_cli_db.csv";

// Writing
if (args.Length > 0 && args[0] == "observe")
{
    String observation = args[1];
    String author = Environment.UserName;
    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    File.AppendAllText(
        filename,
        $"{author},{observation},{timestamp}\n"
    );    
}

// Reading
else
{
    using (StreamReader sr = new StreamReader(filename))
    using (CsvReader csv = new CsvReader(sr, CultureInfo.InvariantCulture))
    {
        csv.Context.RegisterClassMap<CheepMap>();

        IEnumerable<Cheep> cheeps = csv.GetRecords<Cheep>();

        foreach (Cheep cheep in cheeps)
        {
            DateTimeOffset dateTime =
                DateTimeOffset.FromUnixTimeSeconds(cheep.Timestamp);

            String output =
                cheep.Author + " @ " +
                dateTime.ToString("MM/dd/yy HH':'mm':'ss") +
                ": " + cheep.Message;

            Console.WriteLine(output);
        }
    }
}

// Cheep record type which CsvHelper can map CSV fields into
public record Cheep(string Author, string Message, long Timestamp);

public sealed class CheepMap : ClassMap<Cheep>
{
    public CheepMap()
    {
        Parameter("Author").Name("Author");
        Parameter("Message").Name("Observation");
        Parameter("Timestamp").Name("Timestamp");
    }
}