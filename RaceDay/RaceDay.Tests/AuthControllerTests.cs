using Microsoft.AspNetCore.Http;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class AuthControllerTests
{
    private static RegisterRequest ValidRegister(string email = "runner@test.com") => new()
    {
        FullName = "Test Runner",
        Email = email,
        Password = "Password123",
        Role = Roles.Participant
    };

    [Fact]
    public async Task Register_ValidRequest_Returns201AndStoresHashedPassword()
    {
        using var db = TestHelpers.NewContext();
        var controller = new AuthController(db).WithSession();

        var result = await controller.Register(ValidRegister());

        Assert.Equal(201, TestHelpers.Status(result));
        var saved = db.Users.Single();
        Assert.NotEqual("Password123", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123", saved.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        using var db = TestHelpers.NewContext();
        var controller = new AuthController(db).WithSession();
        await controller.Register(ValidRegister("runner@test.com"));

        var result = await controller.Register(ValidRegister("RUNNER@test.com"));

        Assert.Equal(409, TestHelpers.Status(result));
    }

    [Fact]
    public async Task Register_UnknownClub_Returns400()
    {
        using var db = TestHelpers.NewContext();
        var controller = new AuthController(db).WithSession();
        var request = ValidRegister();
        request.ClubId = 999;

        var result = await controller.Register(request);

        Assert.Equal(400, TestHelpers.Status(result));
    }

    [Fact]
    public async Task Login_ValidCredentials_StartsSessionWithRole()
    {
        using var db = TestHelpers.NewContext();
        var controller = new AuthController(db).WithSession();
        await controller.Register(ValidRegister());

        var result = await controller.Login(new LoginRequest { Email = "runner@test.com", Password = "Password123" });

        Assert.Equal(200, TestHelpers.Status(result));
        var session = controller.HttpContext.Session;
        Assert.NotNull(session.GetInt32(SessionKeys.UserId));
        Assert.Equal(Roles.Participant, session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        using var db = TestHelpers.NewContext();
        var controller = new AuthController(db).WithSession();
        await controller.Register(ValidRegister());

        var result = await controller.Login(new LoginRequest { Email = "runner@test.com", Password = "WrongPassword" });

        Assert.Equal(401, TestHelpers.Status(result));
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        using var db = TestHelpers.NewContext();
        var controller = new AuthController(db).WithSession();

        var result = await controller.Login(new LoginRequest { Email = "nobody@test.com", Password = "Password123" });

        Assert.Equal(401, TestHelpers.Status(result));
    }

    [Fact]
    public async Task Logout_ClearsSession()
    {
        using var db = TestHelpers.NewContext();
        var controller = new AuthController(db).WithSession(1, Roles.Participant);

        var result = controller.Logout();

        Assert.Equal(204, TestHelpers.Status(result));
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        await Task.CompletedTask;
    }
}
