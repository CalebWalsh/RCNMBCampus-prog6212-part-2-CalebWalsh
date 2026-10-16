using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class EventsControllerTests
{
    private static EventRequest ValidEvent() => new()
    {
        EventName = "Peninsula Marathon",
        Description = "Scenic coastal race",
        EventDate = DateTime.Today.AddDays(60),
        Location = "Cape Town",
        Distance = 42.2m,
        EventType = "Running"
    };

    [Fact]
    public async Task Create_SetsOrganiserToLoggedInUser()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var controller = new EventsController(db).WithSession(organiser.UserID, Roles.Organiser);

        var result = await controller.Create(ValidEvent());

        Assert.Equal(201, TestHelpers.Status(result));
        Assert.Equal(organiser.UserID, db.Events.Single().OrganiserID);
    }

    [Fact]
    public async Task GetUpcoming_ExcludesPastEvents()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        TestHelpers.AddEvent(db, organiser.UserID, DateTime.Today.AddDays(10));
        TestHelpers.AddEvent(db, organiser.UserID, DateTime.Today.AddDays(-10));
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetUpcoming();

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventResponse>>(ok.Value);
        Assert.Single(events);
    }

    [Fact]
    public async Task GetById_MissingEvent_Returns404()
    {
        using var db = TestHelpers.NewContext();
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetById(99);

        Assert.Equal(404, TestHelpers.Status(result));
    }

    [Fact]
    public async Task Update_ByOwner_UpdatesEvent()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var controller = new EventsController(db).WithSession(organiser.UserID, Roles.Organiser);
        var request = ValidEvent();
        request.EventName = "Renamed Event";

        var result = await controller.Update(ev.EventID, request);

        Assert.Equal(200, TestHelpers.Status(result));
        Assert.Equal("Renamed Event", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Update_ByDifferentOrganiser_Returns403()
    {
        using var db = TestHelpers.NewContext();
        var owner = TestHelpers.AddUser(db, Roles.Organiser);
        var other = TestHelpers.AddUser(db, Roles.Organiser);
        var ev = TestHelpers.AddEvent(db, owner.UserID);
        var controller = new EventsController(db).WithSession(other.UserID, Roles.Organiser);

        var result = await controller.Update(ev.EventID, ValidEvent());

        Assert.Equal(403, TestHelpers.Status(result));
        Assert.Equal("Test Run", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Update_MissingEvent_Returns404()
    {
        using var db = TestHelpers.NewContext();
        var controller = new EventsController(db).WithSession(1, Roles.Organiser);

        var result = await controller.Update(99, ValidEvent());

        Assert.Equal(404, TestHelpers.Status(result));
    }

    [Fact]
    public async Task Delete_ByOwner_RemovesEventAndDependents()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var participant = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        TestHelpers.AddEnrolment(db, participant.UserID, ev.EventID, category.CategoryID);
        var controller = new EventsController(db).WithSession(organiser.UserID, Roles.Organiser);

        var result = await controller.Delete(ev.EventID);

        Assert.Equal(204, TestHelpers.Status(result));
        Assert.Empty(db.Events);
        Assert.Empty(db.Categories);
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Delete_ByDifferentOrganiser_Returns403()
    {
        using var db = TestHelpers.NewContext();
        var owner = TestHelpers.AddUser(db, Roles.Organiser);
        var other = TestHelpers.AddUser(db, Roles.Organiser);
        var ev = TestHelpers.AddEvent(db, owner.UserID);
        var controller = new EventsController(db).WithSession(other.UserID, Roles.Organiser);

        var result = await controller.Delete(ev.EventID);

        Assert.Equal(403, TestHelpers.Status(result));
        Assert.Single(db.Events);
    }
}
