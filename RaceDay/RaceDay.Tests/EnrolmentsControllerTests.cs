using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class EnrolmentsControllerTests
{
    [Fact]
    public async Task Enrol_ValidCategory_Returns201()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var participant = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        var controller = new EnrolmentsController(db).WithSession(participant.UserID, Roles.Participant);

        var result = await controller.Enrol(ev.EventID, new EnrolRequest { CategoryId = category.CategoryID });

        Assert.Equal(201, TestHelpers.Status(result));
        var saved = db.Enrolments.Single();
        Assert.Equal(participant.UserID, saved.ParticipantID);
        Assert.Equal(category.CategoryID, saved.CategoryID);
    }

    [Fact]
    public async Task Enrol_MissingEvent_Returns404()
    {
        using var db = TestHelpers.NewContext();
        var controller = new EnrolmentsController(db).WithSession(1, Roles.Participant);

        var result = await controller.Enrol(99, new EnrolRequest { CategoryId = 1 });

        Assert.Equal(404, TestHelpers.Status(result));
    }

    [Fact]
    public async Task Enrol_MissingCategory_Returns404()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var participant = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var controller = new EnrolmentsController(db).WithSession(participant.UserID, Roles.Participant);

        var result = await controller.Enrol(ev.EventID, new EnrolRequest { CategoryId = 99 });

        Assert.Equal(404, TestHelpers.Status(result));
    }

    [Fact]
    public async Task Enrol_CategoryFromAnotherEvent_Returns400()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var participant = TestHelpers.AddUser(db, Roles.Participant);
        var first = TestHelpers.AddEvent(db, organiser.UserID);
        var second = TestHelpers.AddEvent(db, organiser.UserID);
        var otherCategory = TestHelpers.AddCategory(db, second.EventID);
        var controller = new EnrolmentsController(db).WithSession(participant.UserID, Roles.Participant);

        var result = await controller.Enrol(first.EventID, new EnrolRequest { CategoryId = otherCategory.CategoryID });

        Assert.Equal(400, TestHelpers.Status(result));
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var participant = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        var controller = new EnrolmentsController(db).WithSession(participant.UserID, Roles.Participant);
        await controller.Enrol(ev.EventID, new EnrolRequest { CategoryId = category.CategoryID });

        var result = await controller.Enrol(ev.EventID, new EnrolRequest { CategoryId = category.CategoryID });

        Assert.Equal(409, TestHelpers.Status(result));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_FullCategory_Returns409()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var first = TestHelpers.AddUser(db, Roles.Participant);
        var second = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID, "10km", maxParticipants: 1);
        TestHelpers.AddEnrolment(db, first.UserID, ev.EventID, category.CategoryID);
        var controller = new EnrolmentsController(db).WithSession(second.UserID, Roles.Participant);

        var result = await controller.Enrol(ev.EventID, new EnrolRequest { CategoryId = category.CategoryID });

        Assert.Equal(409, TestHelpers.Status(result));
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyLoggedInParticipantsEnrolments()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var mine = TestHelpers.AddUser(db, Roles.Participant);
        var theirs = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        TestHelpers.AddEnrolment(db, mine.UserID, ev.EventID, category.CategoryID);
        TestHelpers.AddEnrolment(db, theirs.UserID, ev.EventID, category.CategoryID);
        var controller = new EnrolmentsController(db).WithSession(mine.UserID, Roles.Participant);

        var result = await controller.GetMine();

        var ok = Assert.IsType<OkObjectResult>(result);
        var enrolments = Assert.IsAssignableFrom<IEnumerable<EnrolmentResponse>>(ok.Value);
        var single = Assert.Single(enrolments);
        Assert.Equal(mine.UserID, single.ParticipantId);
    }

    [Fact]
    public async Task GetForEvent_ByOwner_ReturnsAllEnrolments()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var first = TestHelpers.AddUser(db, Roles.Participant);
        var second = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        TestHelpers.AddEnrolment(db, first.UserID, ev.EventID, category.CategoryID);
        TestHelpers.AddEnrolment(db, second.UserID, ev.EventID, category.CategoryID);
        var controller = new EnrolmentsController(db).WithSession(organiser.UserID, Roles.Organiser);

        var result = await controller.GetForEvent(ev.EventID);

        var ok = Assert.IsType<OkObjectResult>(result);
        var enrolments = Assert.IsAssignableFrom<IEnumerable<EnrolmentResponse>>(ok.Value);
        Assert.Equal(2, enrolments.Count());
    }

    [Fact]
    public async Task GetForEvent_ByNonOwner_Returns403()
    {
        using var db = TestHelpers.NewContext();
        var owner = TestHelpers.AddUser(db, Roles.Organiser);
        var other = TestHelpers.AddUser(db, Roles.Organiser);
        var ev = TestHelpers.AddEvent(db, owner.UserID);
        var controller = new EnrolmentsController(db).WithSession(other.UserID, Roles.Organiser);

        var result = await controller.GetForEvent(ev.EventID);

        Assert.Equal(403, TestHelpers.Status(result));
    }

    [Fact]
    public async Task Cancel_OwnEnrolment_Returns204()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var participant = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        var enrolment = TestHelpers.AddEnrolment(db, participant.UserID, ev.EventID, category.CategoryID);
        var controller = new EnrolmentsController(db).WithSession(participant.UserID, Roles.Participant);

        var result = await controller.Cancel(enrolment.EnrolmentID);

        Assert.Equal(204, TestHelpers.Status(result));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Cancel_AnotherParticipantsEnrolment_Returns403()
    {
        using var db = TestHelpers.NewContext();
        var organiser = TestHelpers.AddUser(db, Roles.Organiser);
        var owner = TestHelpers.AddUser(db, Roles.Participant);
        var other = TestHelpers.AddUser(db, Roles.Participant);
        var ev = TestHelpers.AddEvent(db, organiser.UserID);
        var category = TestHelpers.AddCategory(db, ev.EventID);
        var enrolment = TestHelpers.AddEnrolment(db, owner.UserID, ev.EventID, category.CategoryID);
        var controller = new EnrolmentsController(db).WithSession(other.UserID, Roles.Participant);

        var result = await controller.Cancel(enrolment.EnrolmentID);

        Assert.Equal(403, TestHelpers.Status(result));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Cancel_MissingEnrolment_Returns404()
    {
        using var db = TestHelpers.NewContext();
        var controller = new EnrolmentsController(db).WithSession(1, Roles.Participant);

        var result = await controller.Cancel(99);

        Assert.Equal(404, TestHelpers.Status(result));
    }
}
