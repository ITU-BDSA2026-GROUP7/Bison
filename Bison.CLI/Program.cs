// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using System.IO;

String filename = "bison_observe_cli_db.csv";

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
else
{
    StreamReader sr = new StreamReader(filename);
    sr.ReadLine();

    String? line = sr.ReadLine();

    while (line != null)
    {   
        String[] values = line.Split(",");

        DateTimeOffset dateTime = 
            DateTimeOffset.FromUnixTimeSeconds(long.Parse(values[2]));
        
        String output = 
            values[0] + " @ " + 
            dateTime.ToString("MM/dd/yy HH':'mm':'ss") + 
            ": " + values[1];

        Console.WriteLine(output);

        line = sr.ReadLine();
    }


    sr.Close();
}