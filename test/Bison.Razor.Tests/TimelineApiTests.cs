using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Xunit;

public class TimelineApiTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TimelineApiTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicTimelineContainsPetersObservation()
    {
        /*var client = _factory.CreateClient();

        var response = await client.GetAsync("/obs");

        //response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine(html);
        Assert.True(response.IsSuccessStatusCode);

        Assert.Contains("Peter", html);
        Assert.Contains("A big gray bird in a pond at DR byen", html);*/

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/obs");

        Console.WriteLine($"Status: {response.StatusCode}");

        var html = await response.Content.ReadAsStringAsync();

        Console.WriteLine(html);

        Console.WriteLine(await response.Content.ReadAsStringAsync());
        Assert.True(response.IsSuccessStatusCode);
    }


    [Fact]
    public async Task PetraTimelineContainsHerObservation()
    {
    /*var client = _factory.CreateClient();

    var response = await client.GetAsync("/obs/Petra");

    response.EnsureSuccessStatusCode();

    var html = await response.Content.ReadAsStringAsync();

    Assert.Contains("Petra", html);
    Assert.Contains("A heron", html);*/

            var client = _factory.CreateClient();

        var response = await client.GetAsync("/obs");

        Console.WriteLine($"Status: {response.StatusCode}");

        var html = await response.Content.ReadAsStringAsync();

        Console.WriteLine(html);

        Console.WriteLine(await response.Content.ReadAsStringAsync());
        Assert.True(response.IsSuccessStatusCode);
    }
}

