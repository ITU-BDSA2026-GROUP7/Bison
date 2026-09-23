public record ObservationViewModel(string Author, string Message, string Timestamp);

public interface IObservationService
{
    public List<ObservationViewModel> GetObservations();
    public List<ObservationViewModel> GetObservationsFromAuthor(string author);
}

public class ObservationService : IObservationService
{
    private readonly DBFacade _db;

    public ObservationService(DBFacade db)
    {
        _db = db;
    }

    public List<ObservationViewModel> GetObservations()
    {
        const string sql = @"
            SELECT u.username AS author, o.text AS message, o.pub_date AS pub_date
            FROM observation o
            JOIN user u ON o.author_id = u.user_id
            ORDER BY o.pub_date DESC";

        var rows = _db.Query(sql);
        return rows.Select(ToViewModel).ToList();
    }

    public List<ObservationViewModel> GetObservationsFromAuthor(string author)
    {
        const string sql = @"
            SELECT u.username AS author, o.text AS message, o.pub_date AS pub_date
            FROM observation o
            JOIN user u ON o.author_id = u.user_id
            WHERE u.username = @author
            ORDER BY o.pub_date DESC";

        var parameters = new Dictionary<string, object> { { "@author", author } };
        var rows = _db.Query(sql, parameters);
        return rows.Select(ToViewModel).ToList();
    }

    private static ObservationViewModel ToViewModel(Dictionary<string, object> row)
    {
        var author = (string)row["author"];
        var message = (string)row["message"];
        var timestamp = UnixTimeStampToDateTimeString(Convert.ToInt64(row["pub_date"]));
        return new ObservationViewModel(author, message, timestamp);
    }

    private static string UnixTimeStampToDateTimeString(long unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp);
        return dateTime.ToString("MM/dd/yy H:mm:ss");
    }
}
