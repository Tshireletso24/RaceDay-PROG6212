using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

/// <summary>
/// Proves role-based access: not logged in = 401, wrong role or not the owner = 403.
/// </summary>
public class RoleAccessTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public RoleAccessTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    private static object NewEventBody() => new
    {
        name = "Some Event",
        eventDate = DateTime.UtcNow.AddDays(30),
        location = "Pretoria",
        distanceKm = 10,
        eventTypeId = 1
    };

    // ---------- Participant tries Organiser-only actions (403) ----------

    [Fact]
    public async Task Participant_CreatingAnEvent_Returns403()
    {
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/events", NewEventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_UpdatingAnEvent_Returns403()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.PutAsJsonAsync($"/api/events/{ev.EventId}", NewEventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_DeletingAnEvent_Returns403()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_AddingACategory_Returns403()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new { categoryName = "Hacked" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewingAnEventsEnrolments_Returns403()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CapturingAResult_Returns403()
    {
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/results", new { enrolmentId = 1, finishTime = "01:30:00", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_UsingMineFilterOnEvents_Returns403()
    {
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.GetAsync("/api/events?mine=true");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Organiser tries Participant-only actions (403) ----------

    [Fact]
    public async Task Organiser_EnrollingInAnEvent_Returns403()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var category = await organiser.Client.CreateCategoryAsync(ev.EventId);

        var response = await organiser.Client.PostAsJsonAsync("/api/enrolments", new { eventId = ev.EventId, categoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewingMyEnrolments_Returns403()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");

        var response = await organiser.Client.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewingMyResults_Returns403()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");

        var response = await organiser.Client.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Organiser who does not own the event (403) ----------

    [Fact]
    public async Task Organiser_UpdatingAnotherOrganisersEvent_Returns403()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var intruder = await _factory.CreateUserAsync("Organiser");
        var ev = await owner.Client.CreateEventAsync();

        var response = await intruder.Client.PutAsJsonAsync($"/api/events/{ev.EventId}", NewEventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_DeletingAnotherOrganisersEvent_Returns403()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var intruder = await _factory.CreateUserAsync("Organiser");
        var ev = await owner.Client.CreateEventAsync();

        var response = await intruder.Client.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewingAnotherOrganisersEnrolments_Returns403()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var intruder = await _factory.CreateUserAsync("Organiser");
        var ev = await owner.Client.CreateEventAsync();

        var response = await intruder.Client.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Not logged in (401) ----------

    [Fact]
    public async Task NotLoggedIn_CreatingAnEvent_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/events", NewEventBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_Enrolling_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/enrolments", new { eventId = 1, categoryId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_ViewingMyResults_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_CapturingAResult_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/results", new { enrolmentId = 1, finishTime = "01:30:00", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}