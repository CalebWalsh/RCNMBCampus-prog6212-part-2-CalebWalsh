using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task GetMe_ReturnsLoggedInUser()
    {
        using var db = TestHelpers.NewContext();
        var user = TestHelpers.AddUser(db, Roles.Participant, "Naledi Dlamini");
        var controller = new UsersController(db).WithSession(user.UserID, user.Role);

        var result = await controller.GetMe();

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal("Naledi Dlamini", body.FullName);
        Assert.Equal(user.Email, body.Email);
    }

    [Fact]
    public async Task UpdateMe_ChangesProfileFields()
    {
        using var db = TestHelpers.NewContext();
        var user = TestHelpers.AddUser(db, Roles.Participant);
        var controller = new UsersController(db).WithSession(user.UserID, user.Role);

        var result = await controller.UpdateMe(new UpdateProfileRequest { FullName = "New Name", PhoneNumber = "0821234567" });

        Assert.Equal(200, TestHelpers.Status(result));
        var saved = db.Users.Single();
        Assert.Equal("New Name", saved.FullName);
        Assert.Equal("0821234567", saved.PhoneNumber);
    }

    [Fact]
    public async Task UpdateMe_UnknownClub_Returns400()
    {
        using var db = TestHelpers.NewContext();
        var user = TestHelpers.AddUser(db, Roles.Participant);
        var controller = new UsersController(db).WithSession(user.UserID, user.Role);

        var result = await controller.UpdateMe(new UpdateProfileRequest { FullName = "New Name", ClubId = 42 });

        Assert.Equal(400, TestHelpers.Status(result));
    }
}
