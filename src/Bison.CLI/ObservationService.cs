using SimpleDB;

public class ObservationService
{
    private readonly IDatabaseRepository<Cheep> _database;

    public ObservationService(IDatabaseRepository<Cheep> database)
    {
        _database = database;
    }

    public void AddObservation(
        string message,
        string author,
        long timestamp,
        string location
        )
    {
        var existing = _database.Read();

        int nextId = existing.Any()
            ? existing.Max(c => c.Id) + 1
            : 1;

        _database.Store(
            new Cheep(
                nextId,
                author,
                message,
                timestamp,
                location
                ));
    }
    public IEnumerable<Cheep> GetObservationsByLocation(string location)
    {
    return _database.Read().Where(o => o.Location == location);
    }   
}