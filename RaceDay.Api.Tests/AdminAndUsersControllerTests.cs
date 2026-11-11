using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class AdminAndUsersControllerTests
{
    [Fact]
    public void GetMe_NoSession_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = new UsersController(db);
        ControllerTestHelper.AttachUser(controller);

        // Act
        var result = controller.GetMe();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void GetMe_WithSession_ReturnsOkWithoutPasswordHash()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var user = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = new UsersController(db);
        ControllerTestHelper.AttachUser(controller, user.Id, Roles.Participant);

        // Act
        var result = controller.GetMe();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("runner@raceday.co.za", json);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret123!", json);
    }

    [Fact]
    public async Task GetProfilePicture_WithSession_ReturnsPrivateBlobContent()
    {
        using var db = TestDbFactory.Create();
        var user = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        user.ProfilePictureUrl = "https://storage.example.test/raceday-images/profile.png";
        db.SaveChanges();

        var blobService = new FakeBlobService([1, 2, 3]);
        var controller = new UsersController(db, blobService);
        ControllerTestHelper.AttachUser(controller, user.Id, Roles.Participant);

        var result = await controller.GetProfilePicture(CancellationToken.None);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal(user.ProfilePictureUrl, blobService.RequestedImageUrl);
        using var content = new MemoryStream();
        await file.FileStream.CopyToAsync(content);
        Assert.Equal(new byte[] { 1, 2, 3 }, content.ToArray());
    }

    [Fact]
    public void CreateDiscipline_AsOrganiser_SavesDiscipline()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "org@raceday.co.za", "Secret123!");
        var controller = new DisciplinesController(db);
        ControllerTestHelper.AttachUser(controller, organiser.Id, Roles.Organiser);

        // Act
        var result = controller.CreateDiscipline(new CreateDisciplineDto { Name = "Running" });

        // Assert
        Assert.IsType<CreatedResult>(result);
        Assert.Single(db.Disciplines);
    }

    [Fact]
    public void CreateDiscipline_DuplicateName_ReturnsConflict()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "org@raceday.co.za", "Secret123!");
        db.Disciplines.Add(new RaceDay.Api.Models.Discipline { Name = "Running" });
        db.SaveChanges();
        var controller = new DisciplinesController(db);
        ControllerTestHelper.AttachUser(controller, organiser.Id, Roles.Organiser);

        // Act
        var result = controller.CreateDiscipline(new CreateDisciplineDto { Name = "Running" });

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Disciplines);
    }

}

file sealed class FakeBlobService(byte[] imageContent) : IBlobService
{
    public bool IsConfigured => true;
    public string? RequestedImageUrl { get; private set; }

    public Task<string> UploadImageAsync(Stream content, string contentType, string fileName, CancellationToken cancellationToken = default) =>
        Task.FromResult("https://storage.example.test/raceday-images/uploaded.png");

    public Task<BlobImage?> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        RequestedImageUrl = imageUrl;
        return Task.FromResult<BlobImage?>(new BlobImage(new MemoryStream(imageContent), "image/png"));
    }
}
