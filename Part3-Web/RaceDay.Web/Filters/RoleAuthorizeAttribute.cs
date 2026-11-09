using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Protects MVC pages using the session.
/// Not logged in = redirected to the login page. Wrong role = redirected to "access denied".
/// No role given = any logged-in user.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public RoleAuthorizeAttribute(string? role = null) => _role = role;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) == null)
        {
            var request = context.HttpContext.Request;
            string? returnUrl = request.Method == "GET" ? request.Path + request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_role != null && session.GetString(SessionKeys.Role) != _role)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
