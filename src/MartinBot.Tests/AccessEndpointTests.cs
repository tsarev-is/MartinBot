using Microsoft.AspNetCore.Mvc.Testing;

namespace MartinBot.Tests;

public sealed class AccessEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestCase("/")]
    [TestCase("/health")]
    public async Task AccessTest(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.That(response.IsSuccessStatusCode, Is.True);
    }
}
