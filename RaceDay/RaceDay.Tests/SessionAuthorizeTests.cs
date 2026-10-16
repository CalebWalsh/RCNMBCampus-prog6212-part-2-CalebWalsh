using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api.Auth;

namespace RaceDay.Tests;

public class SessionAuthorizeTests
{
    private static AuthorizationFilterContext BuildContext(int? userId, string? role)
    {
        var http = new DefaultHttpContext { Session = new TestSession() };
        if (userId.HasValue)
        {
            http.Session.SetInt32(SessionKeys.UserId, userId.Value);
            http.Session.SetString(SessionKeys.Role, role ?? string.Empty);
        }
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void NoSession_Returns401()
    {
        var context = BuildContext(null, null);

        new SessionAuthorizeAttribute().OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void WrongRole_Returns403()
    {
        var context = BuildContext(1, Roles.Participant);

        new SessionAuthorizeAttribute { Role = Roles.Organiser }.OnAuthorization(context);

        var result = Assert.IsAssignableFrom<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public void CorrectRole_Passes()
    {
        var context = BuildContext(1, Roles.Organiser);

        new SessionAuthorizeAttribute { Role = Roles.Organiser }.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void AnyLoggedInUser_PassesWhenNoRoleRequired()
    {
        var context = BuildContext(1, Roles.Participant);

        new SessionAuthorizeAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }
}
