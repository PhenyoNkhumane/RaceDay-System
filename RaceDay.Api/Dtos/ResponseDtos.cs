using System.Text.Json.Serialization;

namespace RaceDay.Api.Dtos;

public sealed class OrganiserEventSummaryDto
{
    public required EventResponseDto Event { get; init; }
    public int EnrolmentCount { get; init; }
    public bool Upcoming { get; init; }
}

public sealed class EventResponseDto
{
    public int Id { get; init; }
    public int OrganiserId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateOnly Date { get; init; }
    public decimal DistanceKm { get; init; }
    public required string EventType { get; init; }
    public required string VenueName { get; init; }
    public required string City { get; init; }
    public required string Province { get; init; }
    public required string Status { get; init; }
    public string? BannerUrl { get; init; }
}

public sealed class CategoryResponseDto
{
    public int Id { get; init; }
    public int EventId { get; init; }
    public int DisciplineId { get; init; }
    public required string Name { get; init; }
    public required string CategoryType { get; init; }
    public decimal? DistanceKm { get; init; }
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }
    public decimal EntryFeeZar { get; init; }
    public int MaxSlots { get; init; }
    public TimeOnly StartTime { get; init; }
}

public sealed class RegistrationResponseDto
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public int? ParticipantId { get; init; }
    public DateTime RegistrationDate { get; init; }
    public required string PaymentStatus { get; init; }
    public decimal AmountPaid { get; init; }
    public string? AssignedBibNumber { get; init; }
    public bool RequiresTempAsaLicense { get; init; }
    public string? MedicalAidName { get; init; }
    public string? MedicalAidNumber { get; init; }
    public required string Status { get; init; }
    public string? CategoryName { get; init; }
    public string? EventName { get; init; }
    public DateOnly? EventDate { get; init; }
    public string? ParticipantName { get; init; }
}

public sealed class ResultResponseDto
{
    public int Id { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RegistrationId { get; init; }
    public int CategoryId { get; init; }
    public TimeOnly? GunTime { get; init; }
    public TimeOnly? NetTime { get; init; }
    public int? OverallPosition { get; init; }
    public int? CategoryPosition { get; init; }
    public required string Status { get; init; }
    public string? EventName { get; init; }
    [JsonIgnore]
    public int? RegistrationEventId { get; init; }
    public DateOnly? EventDate { get; init; }
    public string? CategoryName { get; init; }
    public int? FinisherCount { get; init; }
    public TimeOnly? FinishTime { get; init; }
}

public sealed class ClubResponseDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class DisciplineResponseDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public sealed class MembershipResponseDto
{
    public int Id { get; init; }
    public int ClubId { get; init; }
    public int DisciplineId { get; init; }
    public string? MembershipNumber { get; init; }
    public DateOnly JoinedAt { get; init; }
    public required string Status { get; init; }
}
