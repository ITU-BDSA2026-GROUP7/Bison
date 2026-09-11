using SimpleDB;

public class CommentService
{
    private readonly IDatabaseRepository<Cheep> _database;
    private readonly IDatabaseRepository<Comment> _commentDatabase;

    public CommentService(
        IDatabaseRepository<Cheep> database,
        IDatabaseRepository<Comment> commentDatabase)
    {
        _database = database;
        _commentDatabase = commentDatabase;
    }

    public void AddComment(
        int observationId,
        string message,
        string author,
        long timestamp)
    {
        bool observationExists = _database
            .Read()
            .Any(o => o.Id == observationId);

        if (!observationExists)
        {
            return;
        }

        var existingComments = _commentDatabase.Read();

        int nextCommentId = existingComments.Any()
            ? existingComments.Max(c => c.Id) + 1
            : 1;

        _commentDatabase.Store(
            new Comment(
                nextCommentId,
                author,
                message,
                timestamp,
                observationId));
    }
}