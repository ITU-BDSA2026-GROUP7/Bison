using Bizon.Razor.Repositories;

public record ObservationViewModel(int Id, string Author, string Message, string Timestamp);

public record CommentViewModel(int ObservationId, string Author, string Message, string Timestamp);

public record ProposalViewModel(int ObservationId, string TaxonId, string Author, string Message, string Timestamp);

public record ObservationPage(
    List<ObservationViewModel> Observations,
    bool HasNextPage
);

public record CommentPage(
    List<CommentViewModel> Observations,
    bool HasNextPage
);

public record ProposalPage(
    List<ProposalViewModel> Observations,
    bool HasNextPage
);

public record ObservationDetailsPage(
    ObservationViewModel Observation, CommentPage Comments, ProposalPage Proposals);

public interface IObservationService
{
    public ObservationPage GetObservations(int page);
    public ObservationPage GetObservationsFromAuthor(string author, int page);
    public ObservationViewModel? GetObservation(int id);
}

public class ObservationService : IObservationService
{
    private const int PageSize = 32;

    private readonly DBFacade _db;

    public ObservationService(DBFacade db)
    {
        _db = db;
    }

    public ObservationPage GetObservations(int page)
    {
        const string sql = @"
            SELECT o.observation_id AS observation_id,
                   u.username AS author, o.text AS message, o.pub_date AS pub_date
            FROM observation o
            JOIN user u ON o.author_id = u.user_id
            ORDER BY o.pub_date DESC
            LIMIT @pageSize OFFSET @offset";

        var parameters = new Dictionary<string, object>
        {
            { "@pageSize", PageSize + 1 },
            { "@offset", (page - 1) * PageSize }
        };

        var rows = _db.Query(sql, parameters);

        var hasNextPage = rows.Count > PageSize;

        var observations = rows
            .Take(PageSize)
            .Select(ToViewModel)
            .ToList();

        return new ObservationPage(observations, hasNextPage);
    }

    public ObservationPage GetObservationsFromAuthor(string author, int page)
    {
        const string sql = @"
            SELECT o.observation_id AS observation_id,
                   u.username AS author, o.text AS message, o.pub_date AS pub_date
            FROM observation o
            JOIN user u ON o.author_id = u.user_id
            WHERE u.username = @author
            ORDER BY o.pub_date DESC
            LIMIT @pageSize OFFSET @offset";

        var parameters = new Dictionary<string, object>
        {
            { "@author", author },
            { "@pageSize", PageSize + 1 },
            { "@offset", (page - 1) * PageSize }
        };

        var rows = _db.Query(sql, parameters);

        var hasNextPage = rows.Count > PageSize;

        var observations = rows
            .Take(PageSize)
            .Select(ToViewModel)
            .ToList();

        return new ObservationPage(observations, hasNextPage);
    }

    public ObservationViewModel? GetObservation(int id)
    {
        const string sql = @"
            SELECT o.observation_id AS observation_id,
                   u.username AS author, o.text AS message, o.pub_date AS pub_date
            FROM observation o
            JOIN user u ON o.author_id = u.user_id
            WHERE o.observation_id = @id";

        var parameters = new Dictionary<string, object>
        {
            { "@id", id },
        };

        var row = _db.Query(sql, parameters);

        var observation = row
            .Select(ToViewModel);
            
        return observation.FirstOrDefault();
    }

    private static ObservationViewModel ToViewModel(Dictionary<string, object> row)
    {
        var id = Convert.ToInt32(row["observation_id"]);
        var author = (string)row["author"];
        var message = (string)row["message"];
        var timestamp = Methods.UnixTimeStampToDateTimeString(
            Convert.ToInt64(row["pub_date"])
        );

        return new ObservationViewModel(id, author, message, timestamp);
    }
}

public interface ICommentService
{
    CommentPage GetComments(int observationId, int page);
}

public class CommentService : ICommentService
{
    private const int PageSize = 32;

    private readonly DBFacade _db;

    public CommentService(DBFacade db)
    {
        _db = db;
    }

    public CommentPage GetComments(int observationId, int page) {
        const string sql = @"
            SELECT u.username AS author, c.text AS message, c.pub_date AS pub_date,
            c.observation_id AS observation_id
            FROM comments c
            JOIN user u ON c.author_id = u.user_id
            WHERE c.observation_id = @observationId
            ORDER BY c.pub_date DESC
            LIMIT @pageSize OFFSET @offset";

        var parameters = new Dictionary<string, object>
        {
            { "@observationId", observationId },
            { "@pageSize", PageSize + 1 },
            { "@offset", (page - 1) * PageSize }
        };

        var rows = _db.Query(sql, parameters);

        var hasNextPage = rows.Count > PageSize;

        var comments = rows
            .Take(PageSize)
            .Select(ToViewModel)
            .ToList();

        return new CommentPage(comments, hasNextPage);
    }

    private static CommentViewModel ToViewModel(Dictionary<string, object> row)
    {
        var observationId = Convert.ToInt32(row["observation_id"]);
        var author = (string)row["author"];
        var message = (string)row["message"];
        var timestamp = Methods.UnixTimeStampToDateTimeString(
            Convert.ToInt64(row["pub_date"])
        );

        return new CommentViewModel(observationId, author, message, timestamp);
    }
}

public interface IProposalService
{
    ProposalPage GetProposals(int observationId, int page);
}

public class ProposalService : IProposalService
{
    private const int PageSize = 32;

    private readonly DBFacade _db;

    public ProposalService(DBFacade db)
    {
        _db = db;
    }

    public ProposalPage GetProposals(int observationId, int page) {
        const string sql = @"
            SELECT u.username AS author, p.text AS message, p.pub_date AS pub_date,
            p.observation_id AS observation_id, p.taxon_id AS taxon_id
            FROM proposals p
            JOIN user u ON p.author_id = u.user_id
            WHERE p.observation_id = @observationId
            ORDER BY p.pub_date DESC
            LIMIT @pageSize OFFSET @offset";

        var parameters = new Dictionary<string, object>
        {
            { "@observationId", observationId },
            { "@pageSize", PageSize + 1 },
            { "@offset", (page - 1) * PageSize }
        };

        var rows = _db.Query(sql, parameters);

        var hasNextPage = rows.Count > PageSize;

        var proposals = rows
            .Take(PageSize)
            .Select(ToViewModel)
            .ToList();

        return new ProposalPage(proposals, hasNextPage);
    }

    private static ProposalViewModel ToViewModel(Dictionary<string, object> row)
    {
        var observationId = Convert.ToInt32(row["observation_id"]);
        var taxonId = (string)row["taxon_id"];
        var author = (string)row["author"];
        var message = (string)row["message"];
        var timestamp = Methods.UnixTimeStampToDateTimeString(
            Convert.ToInt64(row["pub_date"])
        );

        return new ProposalViewModel(observationId, taxonId, author, message, timestamp);
    }

}
public static class Methods {
    public static string UnixTimeStampToDateTimeString(long unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(
            1970,
            1,
            1,
            0,
            0,
            0,
            0,
            DateTimeKind.Utc
        );

        dateTime = dateTime.AddSeconds(unixTimeStamp);

        return dateTime.ToString("MM/dd/yy H:mm:ss");
    }
}