using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected ObjectResult Forbidden(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, new { message });
}
