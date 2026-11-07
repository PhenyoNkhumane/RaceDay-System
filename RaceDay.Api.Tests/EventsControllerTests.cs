using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class EventsControllerTests
{
    [Fact]
    public void GetEvent_UnknownId_ReturnsNotFound()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = new EventsController(db);

        // Act
        var result = controller.GetEvent(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void CreateEvent_AsOrganiser_SavesEvent()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "org@raceday.co.za", "Secret123!");
        var controller = new EventsController(db);
        ControllerTestHelper.AttachUser(controller, organiser.Id, Roles.Organiser);

        // Act
        var result = controller.CreateEvent(ValidEvent());

        // Assert
        Assert.IsType<CreatedResult>(result);
        Assert.Single(db.Events);
        Assert.Equal("Johannesburg Road Race", db.Events.Single().Title);
        Assert.Equal(organiser.Id, db.Events.Single().OrganiserId);
    }

    [Fact]
    public void CreateEvent_InvalidType_ReturnsBadRequest()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "org@raceday.co.za", "Secret123!");
        var controller = new EventsController(db);
        ControllerTestHelper.AttachUser(controller, organiser.Id, Roles.Organiser);
        var dto = ValidEvent();
        dto.EventType = "Swim";

        // Act
        var result = controller.CreateEvent(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Events);
    }

    [Fact]
    public void GetMyEvents_AsOrganiser_IncludesDraftEvents()
    {
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "org@raceday.co.za", "Secret123!");
        db.Events.Add(new Event
        {
            OrganiserId = organiser.Id,
            Title = "Draft Race",
            Date = new DateOnly(2027, 10, 10),
            DistanceKm = 10,
            EventType = "Run",
            VenueName = "Zoo Lake",
            City = "Johannesburg",
            Province = "Gauteng",
            Status = "Draft"
        });
        db.SaveChanges();
        var controller = new EventsController(db);
        ControllerTestHelper.AttachUser(controller, organiser.Id, Roles.Organiser);

        var result = controller.GetMyEvents();

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventResponseDto>>(ok.Value);
        Assert.Contains(events, e => e.Status == "Draft" && e.Title == "Draft Race");
    }

    [Fact]
    public void DeleteEvent_UnknownId_ReturnsNotFound()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "org@raceday.co.za", "Secret123!");
        var controller = new EventsController(db);
        ControllerTestHelper.AttachUser(controller, organiser.Id, Roles.Organiser);

        // Act
        var result = controller.DeleteEvent(42);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void DeleteEvent_WrongOwner_ReturnsForbidden()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var owner = ControllerTestHelper.AddOrganiser(db, "owner@raceday.co.za", "Secret123!");
        var other = ControllerTestHelper.AddOrganiser(db, "other@raceday.co.za", "Secret123!");
        db.Events.Add(new Event
        {
            OrganiserId = owner.Id,
            Title = "Owner Event",
            Date = new DateOnly(2027, 10, 10),
            DistanceKm = 10,
            EventType = "Run",
            VenueName = "Zoo Lake",
            City = "Johannesburg",
            Province = "Gauteng",
            Status = "Draft"
        });
        db.SaveChanges();
        var controller = new EventsController(db);
        ControllerTestHelper.AttachUser(controller, other.Id, Roles.Organiser);

        // Act
        var result = controller.DeleteEvent(db.Events.Single().Id);

        // Assert
        var forbidden = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Single(db.Events);
    }

    private static CreateEventDto ValidEvent() => new()
    {
        Title = "Johannesburg Road Race",
        Description = "Annual road running event.",
        Date = new DateOnly(2027, 10, 10),
        VenueName = "Zoo Lake Sports Grounds",
        City = "Johannesburg",
        Province = "Gauteng",
        DistanceKm = 21,
        EventType = "Run",
        Status = "Published"
    };
}
