using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class CategoriesControllerTests
{
    private static CategoryRequest ValidCategory(string name = "21km") => new()
    {
        CategoryName = name,
        MinAge = 16,
        MaxParticipants = 500,
        EntryFee = 250m
    };

    [Fact]
    public async Task GetForEvent_MissingEvent_Returns404()
    {
        using var db = TestHelpers.NewContext();
        var controller = new CategoriesController(db).WithSession();

        var result = await controller.GetForEvent(99);

        Assert.Equal(404, TestHelpers.Status(result));
    }

    [Fact]
    public async Task GetForEvent_ReturnsOnlyThatEventsCategories()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var first = TestHelpers.AddEvent(db, organiser.UserID);
        var second = TestHelpers.AddEvent(db, organiser.UserID);
        TestHelpers.AddCategory(db, first.EventID, "10km");
        TestHelpers.AddCategory(db, second.EventID, "5km");
        var controller = new CategoriesController(db).WithSession();

        var result = await controller.GetForEvent(first.EventID);

        var ok = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<CategoryResponse>>(ok.Value);
        Assert.Single(categories);
    }

    [Fact]
    public async Task Create_ForOwnEvent_Returns201()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var controller = new CategoriesController(db).WithSession(organiser.UserID, Roles.Organiser);

        var result = await controller.Create(ev.EventID, ValidCategory());

        Assert.Equal(201, TestHelpers.Status(result));
        Assert.Single(db.Categories);
    }

    [Fact]
    public async Task Create_ForAnotherOrganisersEvent_Returns403()
    {
        using var db = TestHelpers.NewContext();
        var owner = TestHelpers.AddUser(db, Roles.Organiser);
        var other = TestHelpers.AddUser(db, Roles.Organiser);
        var ev = TestHelpers.AddEvent(db, owner.UserID);
        var controller = new CategoriesController(db).WithSession(other.UserID, Roles.Organiser);

        var result = await controller.Create(ev.EventID, ValidCategory());

        Assert.Equal(403, TestHelpers.Status(result));
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task Create_DuplicateName_Returns400()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        TestHelpers.AddCategory(db, ev.EventID, "21km");
        var controller = new CategoriesController(db).WithSession(organiser.UserID, Roles.Organiser);

        var result = await controller.Create(ev.EventID, ValidCategory("21km"));

        Assert.Equal(400, TestHelpers.Status(result));
    }

    [Fact]
    public async Task Update_ByOwner_ChangesCategory()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID, "10km");
        var controller = new CategoriesController(db).WithSession(organiser.UserID, Roles.Organiser);

        var result = await controller.Update(category.CategoryID, ValidCategory("Half Marathon"));

        Assert.Equal(200, TestHelpers.Status(result));
        Assert.Equal("Half Marathon", db.Categories.Single().CategoryName);
    }

    [Fact]
    public async Task Delete_WithEnrolments_Returns409()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var participant = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        TestHelpers.AddEnrolment(db, participant.UserID, ev.EventID, category.CategoryID);
        var controller = new CategoriesController(db).WithSession(organiser.UserID, Roles.Organiser);

        var result = await controller.Delete(category.CategoryID);

        Assert.Equal(409, TestHelpers.Status(result));
        Assert.Single(db.Categories);
    }

    [Fact]
    public async Task Delete_WithoutEnrolments_Returns204()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        var controller = new CategoriesController(db).WithSession(organiser.UserID, Roles.Organiser);

        var result = await controller.Delete(category.CategoryID);

        Assert.Equal(204, TestHelpers.Status(result));
        Assert.Empty(db.Categories);
    }
}
