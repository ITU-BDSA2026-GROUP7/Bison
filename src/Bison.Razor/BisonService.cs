public record ObservationViewModel(string Author, string Message, string Timestamp);

public record ObservationPage(
    List<ObservationViewModel> Observations,
    bool HasNextPage
);

public interface IObservationService
{
    public ObservationPage GetObservations(int page);
    public ObservationPage GetObservationsFromAuthor(string author, int page);
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
            SELECT u.username AS author, o.text AS message, o.pub_date AS pub_date
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
            SELECT u.username AS author, o.text AS message, o.pub_date AS pub_date
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

    private static ObservationViewModel ToViewModel(Dictionary<string, object> row)
    {
        var author = (string)row["author"];
        var message = (string)row["message"];
        var timestamp = UnixTimeStampToDateTimeString(
            Convert.ToInt64(row["pub_date"])
        );

        return new ObservationViewModel(author, message, timestamp);
    }

    private static string UnixTimeStampToDateTimeString(long unixTimeStamp)
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