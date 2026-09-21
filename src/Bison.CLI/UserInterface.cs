
public static class UserInterface
{
    public static void PrintCheeps(IEnumerable<ObservationRequest> cheeps)
    {
        foreach (ObservationRequest cheep in cheeps)
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

    public static void PrintComments(IEnumerable<CommentRequest> comments)
    {
        foreach (CommentRequest comment in comments)
        {
            DateTimeOffset dateTime =
                DateTimeOffset.FromUnixTimeSeconds(comment.Timestamp);

            string output =
                comment.Author + " @ " +
                dateTime.ToString("MM/dd/yy HH':'mm':'ss") +
                ": " + comment.Message;

            Console.WriteLine(output);
        }
    }
}

public record ObservationRequest(string Author, string Message, long Timestamp, string? Location = null);

public record CommentRequest(string Author, string Message, long Timestamp, int ObservationId, string? Location = null);