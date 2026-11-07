using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

/// <summary>Stands in for Azure Blob Storage during tests, so no real upload happens.</summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadImageAsync(Stream content, string fileName, string contentType, string folder) =>
        Task.FromResult($"https://fake-storage.test/raceday-images/{folder}/{Guid.NewGuid():N}{Path.GetExtension(fileName)}");
}

public class UploadTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public UploadTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    private static MultipartFormDataContent FileContent(string contentType = "image/png", string fileName = "image.png")
    {
        var file = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent();
        form.Add(file, "file", fileName);
        return form;
    }

    [Fact]
    public async Task UploadBanner_AsOwningOrganiser_Returns200_AndSavesTheImageUrl()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();

        var response = await organiser.Client.PostAsync($"/api/events/{ev.EventId}/banner", FileContent());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("/events/", body.GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task UploadBanner_ByAParticipant_Returns403()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.PostAsync($"/api/events/{ev.EventId}/banner", FileContent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ForAnotherOrganisersEvent_Returns403()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var intruder = await _factory.CreateUserAsync("Organiser");
        var ev = await owner.Client.CreateEventAsync();

        var response = await intruder.Client.PostAsync($"/api/events/{ev.EventId}/banner", FileContent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithANonImageFile_Returns400()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();

        var response = await organiser.Client.PostAsync($"/api/events/{ev.EventId}/banner", FileContent("text/plain", "notes.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_NotLoggedIn_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/events/1/banner", FileContent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_AsLoggedInUser_Returns200_AndSavesTheUrl()
    {
        var participant = await _factory.CreateUserAsync("Participant");

        var response = await participant.Client.PostAsync("/api/users/me/picture", FileContent());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("/profiles/", body.GetProperty("profilePictureUrl").GetString());
    }

    [Fact]
    public async Task UploadProfilePicture_NotLoggedIn_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/users/me/picture", FileContent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
