using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class AuthTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public AuthTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@test.com";

    private static object ParticipantBody(string? email = null, string password = RaceDayWebApplicationFactory.Password) => new
    {
        fullName = "Sarah van Wyk",
        email = email ?? NewEmail(),
        password,
        role = "Participant",
        phoneNumber = "0731112222"
    };

    // ---------- Registration ----------

    [Fact]
    public async Task Register_Participant_Returns201_AndNeverReturnsThePassword()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", ParticipantBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Participant", body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_Organiser_WithValidInviteCode_Returns201()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Thabo Nkosi",
            email = NewEmail(),
            password = RaceDayWebApplicationFactory.Password,
            role = "Organiser",
            organiserCode = RaceDayWebApplicationFactory.OrganiserCode
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithoutInviteCode_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Sneaky User",
            email = NewEmail(),
            password = RaceDayWebApplicationFactory.Password,
            role = "Organiser"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithWrongInviteCode_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Sneaky User",
            email = NewEmail(),
            password = RaceDayWebApplicationFactory.Password,
            role = "Organiser",
            organiserCode = "WRONG-CODE"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Admin Wannabe",
            email = NewEmail(),
            password = RaceDayWebApplicationFactory.Password,
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", ParticipantBody(password: "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();

        var first = await client.PostAsJsonAsync("/api/auth/register", ParticipantBody(email));
        var second = await client.PostAsJsonAsync("/api/auth/register", ParticipantBody(email));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_StoresAHashedPassword_NotThePlainText()
    {
        var email = NewEmail();
        await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", ParticipantBody(email));

        var storedHash = _factory.WithDb(db => db.Users.Single(u => u.Email == email).PasswordHash);

        Assert.NotEqual(RaceDayWebApplicationFactory.Password, storedHash);
        Assert.StartsWith("$2", storedHash); // BCrypt hashes start with $2
    }

    // ---------- Login / logout ----------

    [Fact]
    public async Task Login_ValidCredentials_Returns200_WithRole()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", ParticipantBody(email));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = RaceDayWebApplicationFactory.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Participant", user!.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", ParticipantBody(email));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = NewEmail(), password = "Password123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession_SoProtectedEndpointsReturn401()
    {
        var user = await _factory.CreateUserAsync("Participant");

        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await user.Client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await user.Client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Logout_WhenNotLoggedIn_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class ProfileTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public ProfileTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMe_ReturnsTheLoggedInUsersOwnProfile()
    {
        var user = await _factory.CreateUserAsync("Participant");

        var response = await user.Client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(user.Email, profile!.Email);
    }

    [Fact]
    public async Task UpdateMe_ChangesTheNameAndPhoneNumber()
    {
        var user = await _factory.CreateUserAsync("Participant");

        var response = await user.Client.PutAsJsonAsync("/api/users/me", new { fullName = "Updated Name", phoneNumber = "0839998888" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Updated Name", profile!.FullName);
        Assert.Equal("0839998888", profile.PhoneNumber);
    }

    [Fact]
    public async Task GetMe_WhenNotLoggedIn_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}