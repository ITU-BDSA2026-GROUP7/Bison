using SimpleDB;

public class ObservationServiceTests
{
    [Fact]
    public void AddObservation_AssignsNextAvailableId()
    {
        // Arrange
        string observationFile =
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");

        var database = CSVDatabase<Cheep>.Instance(observationFile);

        database.Store(new Cheep(1, "User1", "First", 0));
        database.Store(new Cheep(2, "User2", "Second", 0));
        database.Store(new Cheep(7, "User3", "Seventh", 0));

        var service = new ObservationService(database);

        // Act
        service.AddObservation(
            "New observation",
            "TestUser",
            0);
        
        // Assert
        var observations = database.Read();

        var newObservation = observations.Single(
            o => o.Message == "New observation");

        Assert.Equal(8, newObservation.Id);

        // Clean up
        File.Delete(observationFile);
    }
}