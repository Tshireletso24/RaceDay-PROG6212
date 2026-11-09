using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC with automatic anti-forgery protection on every POST
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));

// Server-side session for the MVC app: remembers who is logged in and their role
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".RaceDay.Web.Session";
});
builder.Services.AddHttpContextAccessor();

// The MVC app never touches the database or Azure. Everything goes through the RaceDay API.
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5261";
builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    // We forward the API session cookie ourselves (per user), so the handler must not manage cookies
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

var app = builder.Build();

// Friendly error page instead of raw exception details
app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
