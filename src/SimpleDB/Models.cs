namespace SimpleDB;

public record Cheep(int Id, string Author, string Message, long Timestamp, string Location);

public record Comment(int Id, string Author, string Message, long Timestamp, int ObservationId, string Location) : Cheep(Id, Author, Message, Timestamp, Location);
