namespace Tests.WebApi;

public class PingTests(WebApiTestFixture fixture) : IClassFixture<WebApiTestFixture>
{
    [Fact]
    public async Task Ping_ReturnsPong()
    {
        var response = await fixture.HttpClient.GetAsync("/ping");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("pong", content);
    }
}
