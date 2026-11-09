namespace RaceDay.Web.Services;

/// <summary>Keys for the values the MVC app keeps in its session.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
    public const string PictureUrl = "PictureUrl";

    /// <summary>The API's own session cookie (name=value), forwarded on every API call.</summary>
    public const string ApiCookie = "ApiCookie";
}
