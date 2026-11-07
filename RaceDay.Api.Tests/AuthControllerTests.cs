using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class AuthControllerTests
{
    private const string ValidOrganiserCode = "ChooseYourOwnCode";

    [Fact]
    public async Task Register_NewParticipant_ReturnsCreatedSavesUserAndStartsSession()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);
        var dto = new RegisterDto
        {
            Email = "runner@raceday.co.za",
            Password = "Secret123!xyz",
            FirstName = "Taylor",
            LastName = "Runner",
            IdNumberOrPassport = "TEST-ID-001",
            Role = Roles.Participant
        };

        // Act
        var result = await controller.Register(dto);

        // Assert
        Assert.IsType<CreatedResult>(result);
        Assert.Single(db.Users);
        Assert.Single(db.Participants);
        var saved = db.Users.Single();
        Assert.Equal(Roles.Participant, saved.Role);
        Assert.NotEqual("Secret123!xyz", saved.PasswordHash);
        Assert.Equal(saved.Id, CurrentUser.GetUserId(controller.HttpContext));
        Assert.Equal(Roles.Participant, CurrentUser.GetRole(controller.HttpContext));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = CreateController(db);
        var dto = new RegisterDto
        {
            Email = "runner@raceday.co.za",
            Password = "Another123!xyz",
            FirstName = "Naledi",
            LastName = "Mokoena",
            IdNumberOrPassport = "TEST-ID-002",
            Role = Roles.Participant
        };

        // Act
        var result = await controller.Register(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(db.Users);
        Assert.Null(CurrentUser.GetUserId(controller.HttpContext));
    }

    [Fact]
    public async Task Register_Organiser_CreatesOrganiserSubtypeAndSessionRole()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.Register(new RegisterDto
        {
            Email = "organiser@example.test",
            Password = "StrongPassword123!",
            FirstName = "Alex",
            LastName = "Example",
            IdNumberOrPassport = "TEST-ORG-ID",
            Role = Roles.Organiser,
            OrganiserCode = ValidOrganiserCode
        });

        // Assert
        Assert.IsType<CreatedResult>(result);
        Assert.Single(db.Organisers);
        Assert.Empty(db.Participants);
        Assert.Equal(Roles.Organiser, db.Users.Single().Role);
        Assert.Equal(Roles.Organiser, CurrentUser.GetRole(controller.HttpContext));
    }

    [Fact]
    public async Task Register_Organiser_WrongCode_ReturnsForbidden()
    {
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);

        var result = await controller.Register(new RegisterDto
        {
            Email = "organiser@example.test",
            Password = "StrongPassword123!",
            FirstName = "Alex",
            LastName = "Example",
            IdNumberOrPassport = "TEST-ORG-ID",
            Role = Roles.Organiser,
            OrganiserCode = "WrongCode"
        });

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.Register(new RegisterDto
        {
            Email = "admin@example.test",
            Password = "StrongPassword123!",
            FirstName = "Eve",
            LastName = "Hacker",
            IdNumberOrPassport = "TEST-ID",
            Role = "Admin"
        });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.Register(new RegisterDto
        {
            Email = "runner@example.test",
            Password = "weak",
            FirstName = "Taylor",
            LastName = "Runner",
            IdNumberOrPassport = "TEST-ID",
            Role = Roles.Participant
        });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkAndStoresUserIdAndRoleInSession()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var user = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = CreateController(db);
        var dto = new LoginDto { Email = "runner@raceday.co.za", Password = "Secret123!" };

        // Act
        var result = await controller.Login(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserResponseDto>(ok.Value);
        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Id, CurrentUser.GetUserId(controller.HttpContext));
        Assert.Equal(Roles.Participant, CurrentUser.GetRole(controller.HttpContext));
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret123!", json);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);
        var dto = new LoginDto { Email = "missing@raceday.co.za", Password = "Secret123!" };

        // Act
        var result = await controller.Login(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid email or password", unauthorized.Value);
        Assert.Null(CurrentUser.GetUserId(controller.HttpContext));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedAndNoSession()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = CreateController(db);
        var dto = new LoginDto { Email = "runner@raceday.co.za", Password = "WrongPassword" };

        // Act
        var result = await controller.Login(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid email or password", unauthorized.Value);
        Assert.Null(CurrentUser.GetUserId(controller.HttpContext));
        Assert.Null(CurrentUser.GetRole(controller.HttpContext));
    }

    [Fact]
    public void Logout_ClearsSession()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var user = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = CreateController(db);
        ControllerTestHelper.AttachUser(controller, user.Id, Roles.Participant);

        // Act
        var result = controller.Logout();

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Null(CurrentUser.GetUserId(controller.HttpContext));
        Assert.Null(CurrentUser.GetRole(controller.HttpContext));
    }

    [Fact]
    public async Task ChangePassword_NoSession_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);
        var dto = new ChangePasswordDto { CurrentPassword = "Secret123!", NewPassword = "NewSecret123!" };

        // Act
        var result = await controller.ChangePassword(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var user = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = CreateController(db);
        ControllerTestHelper.AttachUser(controller, user.Id, Roles.Participant);
        var dto = new ChangePasswordDto { CurrentPassword = "Secret123!", NewPassword = "NewSecret123!" };

        // Act
        var result = await controller.ChangePassword(dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var login = await controller.Login(new LoginDto { Email = "runner@raceday.co.za", Password = "NewSecret123!" });
        Assert.IsType<OkObjectResult>(login);
    }

    private static AuthController CreateController(RaceDay.Api.Data.ApplicationDbContext db)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Registration:OrganiserCode"] = ValidOrganiserCode
            })
            .Build();
        var controller = new AuthController(db, configuration);
        ControllerTestHelper.AttachUser(controller);
        return controller;
    }
}
