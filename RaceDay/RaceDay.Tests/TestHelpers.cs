using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public static class TestHelpers
{
    public static RaceDayContext NewContext()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RaceDayContext(options);
    }

    public static T WithSession<T>(this T controller, int? userId = null, string? role = null) where T : ControllerBase
    {
        var http = new DefaultHttpContext { Session = new TestSession() };
        if (userId.HasValue)
        {
            http.Session.SetInt32(SessionKeys.UserId, userId.Value);
            http.Session.SetString(SessionKeys.Role, role ?? string.Empty);
        }
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    public static int Status(IActionResult result) => result switch
    {
        ObjectResult o => o.StatusCode ?? 200,
        StatusCodeResult s => s.StatusCode,
        _ => throw new InvalidOperationException(result.GetType().Name)
    };

    public static User AddUser(RaceDayContext db, string role, string name = "Test User")
    {
        var user = new User
        {
            FullName = name,
            Email = $"{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash",
            Role = role,
            DateRegistered = DateTime.Now
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Event AddEvent(RaceDayContext db, int organiserId, DateTime? date = null)
    {
        var ev = new Event
        {
            EventName = "Test Run",
            EventDate = (date ?? DateTime.Today.AddDays(30)).Date,
            Location = "Cape Town",
            Distance = 10m,
            EventType = "Running",
            OrganiserID = organiserId,
            DateCreated = DateTime.Now
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    public static Category AddCategory(RaceDayContext db, int eventId, string name = "10km", int? maxParticipants = null)
    {
        var category = new Category
        {
            EventID = eventId,
            CategoryName = name,
            MaxParticipants = maxParticipants,
            EntryFee = 100m
        };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static Enrolment AddEnrolment(RaceDayContext db, int participantId, int eventId, int categoryId)
    {
        var enrolment = new Enrolment
        {
            ParticipantID = participantId,
            EventID = eventId,
            CategoryID = categoryId,
            EnrolmentDate = DateTime.Now
        };
        db.Enrolments.Add(enrolment);
        db.SaveChanges();
        return enrolment;
    }
}
