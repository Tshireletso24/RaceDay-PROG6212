using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

/// <summary>Organisers managing events and categories (the success and failure paths).</summary>
public class EventTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EventTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetEvents_WithoutLoggingIn_Returns200()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201_AndOrganiserOwnsIt()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            name = "Joburg City Marathon",
            description = "Road run",
            eventDate = DateTime.UtcNow.AddDays(30),
            location = "Johannesburg, Gauteng",
            distanceKm = 42.2,
            eventTypeId = 1
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Joburg City Marathon", created!.Name);
        Assert.Equal(organiser.UserId, created.OrganiserId);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_Returns400()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            name = "Bad Event",
            eventDate = DateTime.UtcNow.AddDays(30),
            location = "Cape Town",
            distanceKm = 10,
            eventTypeId = 99
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithMissingName_Returns400()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            eventDate = DateTime.UtcNow.AddDays(30),
            location = "Cape Town",
            distanceKm = 10,
            eventTypeId = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ThatExists_ReturnsItWithAnyone()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync("Public Event");

        var response = await _factory.CreateClient().GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ThatDoesNotExist_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsOwner_Returns200_AndChangesTheEvent()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync("Old Name");

        var response = await organiser.Client.PutAsJsonAsync($"/api/events/{ev.EventId}", new
        {
            name = "New Name",
            eventDate = DateTime.UtcNow.AddDays(40),
            location = "Durban",
            distanceKm = 10,
            eventTypeId = 2
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal("Durban", updated.Location);
    }

    [Fact]
    public async Task UpdateEvent_ThatDoesNotExist_Returns404()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");

        var response = await organiser.Client.PutAsJsonAsync("/api/events/999999", new
        {
            name = "Ghost",
            eventDate = DateTime.UtcNow.AddDays(40),
            location = "Nowhere",
            distanceKm = 5,
            eventTypeId = 1
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsOwner_Returns204_AndTheEventIsGone()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync("Doomed Event");

        var delete = await organiser.Client.DeleteAsync($"/api/events/{ev.EventId}");
        var afterwards = await organiser.Client.GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task GetEvents_Mine_ReturnsOnlyTheOrganisersOwnEvents()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var other = await _factory.CreateUserAsync("Organiser");
        var mine = await organiser.Client.CreateEventAsync("Mine");
        await other.Client.CreateEventAsync("Not Mine");

        var response = await organiser.Client.GetAsync("/api/events?mine=true");
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(events!, e => Assert.Equal(organiser.UserId, e.OrganiserId));
        Assert.Contains(events!, e => e.EventId == mine.EventId);
    }

    [Fact]
    public async Task CreateCategory_AsOwner_Returns201_AndAppearsOnTheEvent()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();

        var response = await organiser.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new { categoryName = "Under 20", minAge = 12, maxAge = 19 });
        var list = await _factory.CreateClient().GetFromJsonAsync<List<CategoryDto>>($"/api/events/{ev.EventId}/categories");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(list!, c => c.CategoryName == "Under 20");
    }

    [Fact]
    public async Task CreateCategory_WithMinAgeAboveMaxAge_Returns400()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();

        var response = await organiser.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new { categoryName = "Broken", minAge = 50, maxAge = 20 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_ForMissingEvent_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events/999999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}