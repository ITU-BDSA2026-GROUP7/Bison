// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using System.IO;
String line;

StreamReader sr = new StreamReader("C:\\Users\\AB\\Downloads\\bison_observe_cli_db.csv");
line = sr.ReadLine();
while (line != null)
{
    Console.WriteLine(line);
    line = sr.ReadLine();
}
sr.Close();
Console.ReadLine();