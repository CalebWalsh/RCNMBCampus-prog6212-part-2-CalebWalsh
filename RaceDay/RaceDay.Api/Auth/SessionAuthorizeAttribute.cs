using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string? Role { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) is null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Login required." });
            return;
        }

        if (Role is not null && session.GetString(SessionKeys.Role) != Role)
        {
            context.Result = new ObjectResult(new { message = "You do not have permission to access this resource." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
