// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using System.IO;
String line;

StreamReader sr = new StreamReader("bison_observe_cli_db.csv");
sr.ReadLine();
line = sr.ReadLine();
while (line != null)
{   
    String[] values = line.Split(",");
    DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(values[2]));
    String output = values[0] + " @ " + dateTime.ToString("MM/dd/yy HH:mm:ss") + ": " + values[1];
    Console.WriteLine(output);
    line = sr.ReadLine();
}
sr.Close();