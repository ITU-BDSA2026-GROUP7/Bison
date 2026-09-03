
public static class UserInterface
{
    public static void PrintCheeps(IEnumerable<Cheep> cheeps)
    {
        foreach (Cheep cheep in cheeps)
        {
            DateTimeOffset dateTime =
                DateTimeOffset.FromUnixTimeSeconds(cheep.Timestamp);

            string output =
                cheep.Author + " @ " +
                dateTime.ToString("MM/dd/yy HH':'mm':'ss") +
                ": " + cheep.Message;

            Console.WriteLine(output);
        }
    }
}