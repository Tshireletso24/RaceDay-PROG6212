using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

/// <summary>A registered, logged-in user with their own HttpClient (and therefore their own session cookie).</summary>
public record TestUser(HttpClient Client, string Email, int UserId);

// Minimal copies of the API's response shapes, used to read JSON in tests
public record UserDto(int UserId, string FullName, string Email, string Role, string? PhoneNumber);
public record CategoryDto(int CategoryId, int EventId, string CategoryName);
public record EventDto(int EventId, int OrganiserId, string Name, string Location, int EnrolmentCount, List<CategoryDto> Categories);
public record EnrolmentDto(int EnrolmentId, int EventId, int CategoryId, int ParticipantId, string ParticipantName, string EventName, string Status);
public record ResultDto(int ResultId, string EventName, string CategoryName, string FinishTime, int FinishPosition, int TotalFinishers);

/// <summary>
/// Starts the real API in memory, but swaps SQL Server for an in-memory database
/// so tests are fast and never touch the real database.
/// </summary>
public class RaceDayWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string OrganiserCode = "TEST-ORG-CODE";
    public const string Password = "Password123";

    private readonly string _dbName = $"RaceDayTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrganiserInviteCode"] = OrganiserCode,
                ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused"
            }));

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registration for RaceDayDbContext...
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration") &&
                 d.ServiceType.GenericTypeArguments[0] == typeof(RaceDayDbContext))).ToList();
            foreach (var descriptor in toRemove) services.Remove(descriptor);

            // ...and use an in-memory database instead
            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        // Creates the in-memory database and applies the seeded event types (Run, Walk, Cycle)
        scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Database.EnsureCreated();
        return host;
    }

    /// <summary>Registers a new user with the given role and logs them in.</summary>
    public async Task<TestUser> CreateUserAsync(string role)
    {
        var client = CreateClient();
        var email = $"{role.ToLower()}-{Guid.NewGuid():N}@test.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = $"Test {role}",
            email,
            password = Password,
            role,
            phoneNumber = "0821234567",
            organiserCode = role == "Organiser" ? OrganiserCode : null
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var user = await login.Content.ReadFromJsonAsync<UserDto>();

        return new TestUser(client, email, user!.UserId);
    }

    /// <summary>Runs a query directly against the test database.</summary>
    public T WithDb<T>(Func<RaceDayDbContext, T> query)
    {
        using var scope = Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<RaceDayDbContext>());
    }
}

public static class TestHelpers
{
    public static async Task<EventDto> CreateEventAsync(this HttpClient organiser, string name = "Test Marathon", DateTime? date = null, int eventTypeId = 1)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name,
            description = "A test event",
            eventDate = date ?? DateTime.UtcNow.AddDays(30),
            location = "Johannesburg, Gauteng",
            distanceKm = 21.1,
            eventTypeId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public static async Task<CategoryDto> CreateCategoryAsync(this HttpClient organiser, int eventId, string name = "21km")
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new { categoryName = name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static async Task<EnrolmentDto> EnrolAsync(this HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }
}