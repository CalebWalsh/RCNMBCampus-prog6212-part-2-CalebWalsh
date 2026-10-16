using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class ApiPipelineTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ApiPipelineTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> LoggedInClient(string role)
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.com";

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Test User", email, password = "Password123", role });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        return client;
    }

    private static object NewEvent() => new
    {
        eventName = "Pipeline Run",
        description = "Test event",
        eventDate = "2030-01-01",
        location = "Cape Town",
        distance = 10,
        eventType = "Running"
    };

    [Fact]
    public async Task Anonymous_GetProfile_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_GetEvents_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Test User", email = $"{Guid.NewGuid():N}@test.com", password = "Password123", role = "Admin" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoggedInUser_GetProfile_Returns200()
    {
        var client = await LoggedInClient("Participant");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CreateEvent_Returns403()
    {
        var client = await LoggedInClient("Participant");

        var response = await client.PostAsJsonAsync("/api/events", NewEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_Returns201()
    {
        var client = await LoggedInClient("Organiser");

        var response = await client.PostAsJsonAsync("/api/events", NewEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_Enrol_Returns403()
    {
        var client = await LoggedInClient("Organiser");

        var response = await client.PostAsJsonAsync("/api/events/1/enrolments", new { categoryId = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var client = await LoggedInClient("Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        var profile = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }
}
