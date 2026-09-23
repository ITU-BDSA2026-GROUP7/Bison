using SimpleDB;
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

    public static void PrintProposals(IEnumerable<ProposalRequest> proposals, TaxonomyRepository taxonomy)
{
    foreach (ProposalRequest proposal in proposals)
    {
        DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeSeconds(proposal.Timestamp);
        var taxon = taxonomy.GetById(proposal.TaxonId);
        string name = taxon?.VernacularName ?? taxon?.ScientificName ?? proposal.TaxonId;

        string output =
            proposal.Author + " @ " +
            dateTime.ToString("MM/dd/yy HH':'mm':'ss") +
            ": " + name;

        Console.WriteLine(output);
    }
}
}

public record ObservationRequest(string Author, string Message, long Timestamp, string? Location = null);

public record CommentRequest(string Author, string Message, long Timestamp, int ObservationId, string? Location = null);

public record ProposalRequest(string Author, string TaxonId, long Timestamp, int ObservationId);