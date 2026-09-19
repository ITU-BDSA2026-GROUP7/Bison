
public class UserInterfaceTests
{
    [Fact]
    public void PrintCheeps_ConvertsUnixTimestampToReadableDate()
    {
        // Arrange
        var cheep = new CommentRequest(
            "TestUser",
            "Hello world",
            1725625800,
            1,
            "SomeLocation");

        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        UserInterface.PrintComments(new[] { cheep });

        // Assert
        string result = output.ToString();

        Assert.Contains(
            "09/06/24 12:30:00",
            result);
    }
}