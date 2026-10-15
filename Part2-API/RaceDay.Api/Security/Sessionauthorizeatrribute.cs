using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

/// <summary>Keys used to store the logged-in user's details in the session.</summary>
public static class SessionKeys
{
	public const string UserId = "UserId";
	public const string Role = "Role";
}

/// <summary>
/// Protects an endpoint using the server-side session.
/// No role given = any logged-in user. A role given = only that role.
/// Returns 401 if not logged in, 403 if logged in with the wrong role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
	private readonly string? _role;

	public SessionAuthorizeAttribute(string? role = null)
	{
		_role = role;
	}

	public void OnAuthorization(AuthorizationFilterContext context)
	{
		var session = context.HttpContext.Session;
		var userId = session.GetInt32(SessionKeys.UserId);

		if (userId == null)
		{
			context.Result = new ObjectResult(new { message = "You must be logged in." })
			{ StatusCode = StatusCodes.Status401Unauthorized };
			return;
		}

		if (_role != null && session.GetString(SessionKeys.Role) != _role)
		{
			context.Result = new ObjectResult(new { message = $"This action requires the {_role} role." })
			{ StatusCode = StatusCodes.Status403Forbidden };
		}
	}
}