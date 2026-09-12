using SimpleDB;

public class CommentServiceTests
{
    [Fact]
    public void addComment_DoesNotStoreComment_WhenObservationDoesNotExist()
    {
         // Arrange
        string observationFile =
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");

        string commentFile =
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");

        var database = CSVDatabase<Cheep>.Instance(observationFile);
        var commentDatabase = CSVDatabase<Comment>.Instance(commentFile);

        var service = new CommentService(database, commentDatabase);

        // Act
        service.AddComment(
            observationId: 999,
            message: "This should not be stored",
            author: "TestUser",
            timestamp: 0,
            location: "SomeLocation");
        
    
        // Assert
        Assert.Empty(commentDatabase.Read());

        // Clean up
        File.Delete(observationFile);
        File.Delete(commentFile);
    }
}