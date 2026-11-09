using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public sealed class DisciplineVm { public int Id { get; set; } public string Name { get; set; } = ""; }

public sealed class UserVm
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}
public sealed class EventVm
{
    public int Id { get; set; }
    public int OrganiserId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "Run";
    public string VenueName { get; set; } = "";
    public string City { get; set; } = "";
    public string Province { get; set; } = "";
    public string Status { get; set; } = "Published";
    public string? BannerUrl { get; set; }
}
public sealed class CategoryVm
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int DisciplineId { get; set; }
    public string Name { get; set; } = "";
    public string CategoryType { get; set; } = "Distance";
    public decimal? DistanceKm { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal EntryFeeZar { get; set; }
    public int MaxSlots { get; set; }
    public TimeOnly StartTime { get; set; }
}
public sealed class RegistrationVm
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int? ParticipantId { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public decimal AmountPaid { get; set; }
    public string? AssignedBibNumber { get; set; }
    public string Status { get; set; } = "Active";
    public string? CategoryName { get; set; }
    public string? EventName { get; set; }
    public DateOnly? EventDate { get; set; }
    public string? ParticipantName { get; set; }
}
public sealed class ResultVm
{
    public int Id { get; set; }
    public int? RegistrationId { get; set; }
    public int CategoryId { get; set; }
    public TimeOnly? GunTime { get; set; }
    public TimeOnly? NetTime { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public int? FinisherCount { get; set; }
    public string Status { get; set; } = "Finished";
    public string? EventName { get; set; }
    public DateOnly? EventDate { get; set; }
    public string? CategoryName { get; set; }
}
public sealed class EventSummaryVm
{
    public EventVm Event { get; set; } = new();
    public int EnrolmentCount { get; set; }
    public bool Upcoming { get; set; }
}
public sealed class RegisterVm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(12)] public string Password { get; set; } = "";
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [Required] public string IdNumberOrPassport { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    public string? AsaPermanentLicenseNumber { get; set; }
    public string? OrganiserCode { get; set; }
}
public sealed class LoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
public sealed class EventFormVm
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(1000)] public string? Description { get; set; }
    [Required] public DateOnly Date { get; set; }
    [Required] public string VenueName { get; set; } = "";
    [Required] public string City { get; set; } = "";
    [Required] public string Province { get; set; } = "";
    [Range(0.1, 9999)] public decimal DistanceKm { get; set; }
    [Required] public string EventType { get; set; } = "Run";
    [Required] public string Status { get; set; } = "Draft";
    public string? BannerUrl { get; set; }
    public IFormFile? Banner { get; set; }
}
public sealed class CategoryFormVm
{
    public int Id { get; set; }
    public int EventId { get; set; }
    [Required] public string Name { get; set; } = "";
    [Required] public string CategoryType { get; set; } = "Distance";
    [Range(0.1, 9999)] public decimal? DistanceKm { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    [Range(0, 100000)] public decimal EntryFeeZar { get; set; }
    [Range(1, 100000)] public int MaxSlots { get; set; }
    public TimeOnly StartTime { get; set; }
    [Range(1, int.MaxValue)] public int DisciplineId { get; set; }
}
public sealed class EnrolFormVm { [Required] public int CategoryId { get; set; } public bool RequiresTempAsaLicense { get; set; } public string? MedicalAidName { get; set; } public string? MedicalAidNumber { get; set; } }
public sealed class ResultFormVm
{
    [Required] public int RegistrationId { get; set; }
    public TimeOnly? GunTime { get; set; }
    public TimeOnly? NetTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    [Required] public string Status { get; set; } = "Finished";
}
public sealed class ProfileVm
{
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    public string? IdNumberOrPassport { get; set; }
    public string? AsaPermanentLicenseNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public IFormFile? ProfilePicture { get; set; }
}
public sealed class EventListVm { public List<EventVm> Events { get; set; } = []; public string? Type { get; set; } public DateOnly? From { get; set; } public decimal? MaxDistance { get; set; } }
public sealed class EventDetailVm { public EventVm Event { get; set; } = new(); public List<CategoryVm> Categories { get; set; } = []; }
