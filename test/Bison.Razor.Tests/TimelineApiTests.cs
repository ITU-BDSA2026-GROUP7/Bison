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
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/obs");

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();


        Assert.Contains("Peter", html);
        Assert.Contains("A big bird", html);
    }


    [Fact]
    public async Task PetraTimelineContainsHerObservation()
    {
    var client = _factory.CreateClient();

    var response = await client.GetAsync("/obs/Eduard");

    response.EnsureSuccessStatusCode();

    var html = await response.Content.ReadAsStringAsync();

    Assert.Contains("Eduard", html);
    Assert.Contains("A heron", html);

    }
}

