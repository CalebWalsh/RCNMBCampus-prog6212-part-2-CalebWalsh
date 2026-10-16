using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class ResultsControllerTests
{
    private static ResultRequest ValidResult() => new()
    {
        FinishTime = new TimeSpan(1, 48, 32),
        Position = 214
    };

    [Fact]
    public async Task Capture_ByEventOrganiser_Returns201()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var participant = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        var enrolment = TestHelpers.AddEnrolment(db, participant.UserID, ev.EventID, category.CategoryID);
        var controller = new ResultsController(db).WithSession(organiser.UserID, Roles.Organiser);

        var result = await controller.Capture(enrolment.EnrolmentID, ValidResult());

        Assert.Equal(201, TestHelpers.Status(result));
        var saved = db.Results.Single();
        Assert.Equal(new TimeSpan(1, 48, 32), saved.FinishTime);
        Assert.Equal(214, saved.Position);
    }

    [Fact]
    public async Task Capture_Twice_Returns409()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var participant = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        var enrolment = TestHelpers.AddEnrolment(db, participant.UserID, ev.EventID, category.CategoryID);
        var controller = new ResultsController(db).WithSession(organiser.UserID, Roles.Organiser);
        await controller.Capture(enrolment.EnrolmentID, ValidResult());

        var result = await controller.Capture(enrolment.EnrolmentID, ValidResult());

        Assert.Equal(409, TestHelpers.Status(result));
        Assert.Single(db.Results);
    }

    [Fact]
    public async Task Capture_ByDifferentOrganiser_Returns403()
    {
        using var db = TestHelpers.NewContext();
        var owner = TestHelpers.AddUser(db, Roles.Organiser);
        var other = TestHelpers.AddUser(db, Roles.Organiser);
        var participant = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, owner.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        var enrolment = TestHelpers.AddEnrolment(db, participant.UserID, ev.EventID, category.CategoryID);
        var controller = new ResultsController(db).WithSession(other.UserID, Roles.Organiser);

        var result = await controller.Capture(enrolment.EnrolmentID, ValidResult());

        Assert.Equal(403, TestHelpers.Status(result));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task Capture_MissingEnrolment_Returns404()
    {
        using var db = TestHelpers.NewContext();
        var controller = new ResultsController(db).WithSession(1, Roles.Organiser);

        var result = await controller.Capture(99, ValidResult());

        Assert.Equal(404, TestHelpers.Status(result));
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyLoggedInParticipantsResults()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var mine = TestHelpers.AddUser(db, Roles.Participant);
        var theirs = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        var myEnrolment = TestHelpers.AddEnrolment(db, mine.UserID, ev.EventID, category.CategoryID);
        var theirEnrolment = TestHelpers.AddEnrolment(db, theirs.UserID, ev.EventID, category.CategoryID);
        var organiserController = new ResultsController(db).WithSession(organiser.UserID, Roles.Organiser);
        await organiserController.Capture(myEnrolment.EnrolmentID, ValidResult());
        await organiserController.Capture(theirEnrolment.EnrolmentID, new ResultRequest { FinishTime = new TimeSpan(2, 0, 0), Position = 300 });
        var controller = new ResultsController(db).WithSession(mine.UserID, Roles.Participant);

        var result = await controller.GetMine();

        var ok = Assert.IsType<OkObjectResult>(result);
        var results = Assert.IsAssignableFrom<IEnumerable<ResultResponse>>(ok.Value);
        var single = Assert.Single(results);
        Assert.Equal(214, single.Position);
    }

    [Fact]
    public async Task GetForEvent_OrdersByPosition()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var first = TestHelpers.AddUser(db, Roles.Participant);
        var second = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        var firstEnrolment = TestHelpers.AddEnrolment(db, first.UserID, ev.EventID, category.CategoryID);
        var secondEnrolment = TestHelpers.AddEnrolment(db, second.UserID, ev.EventID, category.CategoryID);
        var organiserController = new ResultsController(db).WithSession(organiser.UserID, Roles.Organiser);
        await organiserController.Capture(firstEnrolment.EnrolmentID, new ResultRequest { FinishTime = new TimeSpan(2, 0, 0), Position = 5 });
        await organiserController.Capture(secondEnrolment.EnrolmentID, new ResultRequest { FinishTime = new TimeSpan(1, 30, 0), Position = 1 });
        var controller = new ResultsController(db).WithSession(first.UserID, Roles.Participant);

        var result = await controller.GetForEvent(ev.EventID);

        var ok = Assert.IsType<OkObjectResult>(result);
        var results = Assert.IsAssignableFrom<IEnumerable<ResultResponse>>(ok.Value).ToList();
        Assert.Equal(new int?[] { 1, 5 }, results.Select(r => r.Position).ToArray());
    }

    [Fact]
    public async Task GetForEvent_MissingEvent_Returns404()
    {
        using var db = TestHelpers.NewContext();
        var controller = new ResultsController(db).WithSession(1, Roles.Participant);

        var result = await controller.GetForEvent(99);

        Assert.Equal(404, TestHelpers.Status(result));
    }
}
