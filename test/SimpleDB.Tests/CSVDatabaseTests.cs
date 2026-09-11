using SimpleDB;

public record TestRecord(string Name, int Value);

public class CSVDatabaseTests
{
    [Fact]
    public void Store_ThenRead_ReturnsStoredRecord()
    {
        // Arrange
        string filePath = 
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
        
        var database = CSVDatabase<TestRecord>.Instance(filePath);

        // Act
        var record = new TestRecord("Alice", 42);
        
        database.Store(record);

        var records = database.Read();

        var storedRecord = records.Single();

        // Assert
        Assert.Equal(record, storedRecord);
    }

    [Fact]
    public void Read_WithLimit_ReturnsOnlyRequestedNumberOfRecords()
    {
        // Arrange
        string filePath = 
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");

        var database = CSVDatabase<TestRecord>.Instance(filePath);

        database.Store(new TestRecord("Alice", 42));
        database.Store(new TestRecord("Bob", 24));
        database.Store(new TestRecord("Charlie", 36));

        // Act
        var records = database.Read(limit: 2).ToList();

        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("Alice", records[0].Name);
        Assert.Equal("Bob", records[1].Name);

        // Clean up
        File.Delete(filePath);    
    }
}