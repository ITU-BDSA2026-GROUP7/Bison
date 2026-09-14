public class UserInterfaceTests
{
    [Fact]
    public void PrintCheeps_ConvertsUnixTimestampToReadableDate()
    {
        // Arrange
        var cheep = new Cheep(
            1,
            "TestUser",
            "Hello world",
            1725625800,
            "SomeLocation");

        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        UserInterface.PrintCheeps(new[] { cheep });

        // Assert
        string result = output.ToString();

        Assert.Contains(
            "09/06/24 12:30:00",
            result);
    }
}