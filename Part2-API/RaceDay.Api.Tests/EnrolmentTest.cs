using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

/// <summary>Participants enrolling in events, and the enrolment being recorded correctly.</summary>
public class EnrolmentTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EnrolmentTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    private async Task<(TestUser Organiser, EventDto Event, CategoryDto Category)> SetUpEventAsync()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var category = await organiser.Client.CreateCategoryAsync(ev.EventId);
        return (organiser, ev, category);
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201_WithPendingStatus()
    {
        var (_, ev, category) = await SetUpEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new { eventId = ev.EventId, categoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal("Pending", enrolment!.Status);
        Assert.Equal(participant.UserId, enrolment.ParticipantId);
    }

    [Fact]
    public async Task Enrol_IsRecordedInTheDatabase_LinkingParticipantEventAndCategory()
    {
        var (_, ev, category) = await SetUpEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");

        await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var saved = _factory.WithDb(db => db.Enrolments.Single(e => e.ParticipantId == participant.UserId));
        Assert.Equal(ev.EventId, saved.EventId);
        Assert.Equal(category.CategoryId, saved.CategoryId);
    }

    [Fact]
    public async Task Enrol_AppearsInTheParticipantsOwnEnrolments()
    {
        var (_, ev, category) = await SetUpEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");
        await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var mine = await participant.Client.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");

        Assert.Single(mine!);
        Assert.Equal(ev.EventId, mine![0].EventId);
    }

    [Fact]
    public async Task Enrol_IsVisibleToTheOwningOrganiser()
    {
        var (organiser, ev, category) = await SetUpEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");
        await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var list = await organiser.Client.GetFromJsonAsync<List<EnrolmentDto>>($"/api/events/{ev.EventId}/enrolments");

        Assert.Single(list!);
        Assert.Equal(participant.UserId, list![0].ParticipantId);
    }

    [Fact]
    public async Task Enrol_TwiceInTheSameEvent_Returns409()
    {
        var (_, ev, category) = await SetUpEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");
        await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var second = await participant.Client.PostAsJsonAsync("/api/enrolments", new { eventId = ev.EventId, categoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithACategoryFromAnotherEvent_Returns400()
    {
        var (_, ev, _) = await SetUpEventAsync();
        var (_, _, otherCategory) = await SetUpEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new { eventId = ev.EventId, categoryId = otherCategory.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InAnEventThatDoesNotExist_Returns400()
    {
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new { eventId = 999999, categoryId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InAPastEvent_Returns400()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var pastEvent = await organiser.Client.CreateEventAsync("Old Race", DateTime.UtcNow.AddDays(-10));
        var category = await organiser.Client.CreateCategoryAsync(pastEvent.EventId);
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new { eventId = pastEvent.EventId, categoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_AsOwningOrganiser_ConfirmsTheEnrolment()
    {
        var (organiser, ev, category) = await SetUpEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");
        var enrolment = await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var response = await organiser.Client.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status", new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal("Confirmed", updated!.Status);
    }

    [Fact]
    public async Task UpdateStatus_WithAnInvalidStatus_Returns400()
    {
        var (organiser, ev, category) = await SetUpEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");
        var enrolment = await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var response = await organiser.Client.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status", new { status = "Maybe" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ByAParticipant_Returns403()
    {
        var (_, ev, category) = await SetUpEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");
        var enrolment = await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);

        var response = await participant.Client.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status", new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

/// <summary>Organisers capturing results and Participants viewing their own race history.</summary>
public class ResultTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public ResultTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    private async Task<(TestUser Organiser, TestUser Participant, EnrolmentDto Enrolment)> SetUpEnrolmentAsync()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var category = await organiser.Client.CreateCategoryAsync(ev.EventId);
        var participant = await _factory.CreateUserAsync("Participant");
        var enrolment = await participant.Client.EnrolAsync(ev.EventId, category.CategoryId);
        return (organiser, participant, enrolment);
    }

    [Fact]
    public async Task CaptureResult_AsOwningOrganiser_Returns201()
    {
        var (organiser, _, enrolment) = await SetUpEnrolmentAsync();

        var response = await organiser.Client.PostAsJsonAsync("/api/results", new { enrolmentId = enrolment.EnrolmentId, finishTime = "01:45:32", finishPosition = 47 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Participant_SeesTheirOwnResult_WithPositionAndTotalFinishers()
    {
        var (organiser, participant, enrolment) = await SetUpEnrolmentAsync();
        await organiser.Client.PostAsJsonAsync("/api/results", new { enrolmentId = enrolment.EnrolmentId, finishTime = "01:45:32", finishPosition = 47 });

        var results = await participant.Client.GetFromJsonAsync<List<ResultDto>>("/api/results/me");

        Assert.Single(results!);
        Assert.Equal("01:45:32", results![0].FinishTime);
        Assert.Equal(47, results[0].FinishPosition);
        Assert.Equal(1, results[0].TotalFinishers);
    }

    [Fact]
    public async Task Participant_DoesNotSeeAnotherParticipantsResult()
    {
        var (organiser, _, enrolment) = await SetUpEnrolmentAsync();
        await organiser.Client.PostAsJsonAsync("/api/results", new { enrolmentId = enrolment.EnrolmentId, finishTime = "01:10:00", finishPosition = 3 });
        var someoneElse = await _factory.CreateUserAsync("Participant");

        var results = await someoneElse.Client.GetFromJsonAsync<List<ResultDto>>("/api/results/me");

        Assert.Empty(results!);
    }

    [Fact]
    public async Task CaptureResult_WithABadTimeFormat_Returns400()
    {
        var (organiser, _, enrolment) = await SetUpEnrolmentAsync();

        var response = await organiser.Client.PostAsJsonAsync("/api/results", new { enrolmentId = enrolment.EnrolmentId, finishTime = "fast", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, enrolment) = await SetUpEnrolmentAsync();
        var body = new { enrolmentId = enrolment.EnrolmentId, finishTime = "01:45:32", finishPosition = 47 };
        await organiser.Client.PostAsJsonAsync("/api/results", body);

        var second = await organiser.Client.PostAsJsonAsync("/api/results", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForAnEnrolmentThatDoesNotExist_Returns404()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/results", new { enrolmentId = 999999, finishTime = "01:00:00", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForAnotherOrganisersEvent_Returns403()
    {
        var (_, _, enrolment) = await SetUpEnrolmentAsync();
        var intruder = await _factory.CreateUserAsync("Organiser");

        var response = await intruder.Client.PostAsJsonAsync("/api/results", new { enrolmentId = enrolment.EnrolmentId, finishTime = "01:00:00", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}