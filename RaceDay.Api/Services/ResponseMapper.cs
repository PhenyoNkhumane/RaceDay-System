using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public static class ResponseMapper
{
    public static EventResponseDto ToResponse(Event ev) => new()
    {
        Id = ev.Id,
        OrganiserId = ev.OrganiserId,
        Title = ev.Title,
        Description = ev.Description,
        Date = ev.Date,
        DistanceKm = ev.DistanceKm,
        EventType = ev.EventType,
        VenueName = ev.VenueName,
        City = ev.City,
        Province = ev.Province,
        Status = ev.Status,
        BannerUrl = ev.BannerUrl
    };

    public static CategoryResponseDto ToResponse(Category category) => new()
    {
        Id = category.Id,
        EventId = category.EventId,
        DisciplineId = category.DisciplineId,
        Name = category.Name,
        CategoryType = category.CategoryType,
        DistanceKm = category.DistanceKm,
        MinAge = category.MinAge,
        MaxAge = category.MaxAge,
        EntryFeeZar = category.EntryFeeZar,
        MaxSlots = category.MaxSlots,
        StartTime = category.StartTime
    };

    public static RegistrationResponseDto ToResponse(Registration registration, bool includeMedicalAid = false) => new()
    {
        Id = registration.Id,
        CategoryId = registration.CategoryId,
        ParticipantId = registration.ParticipantId,
        RegistrationDate = registration.RegistrationDate,
        PaymentStatus = registration.PaymentStatus,
        AmountPaid = registration.AmountPaid,
        AssignedBibNumber = registration.AssignedBibNumber,
        RequiresTempAsaLicense = registration.RequiresTempAsaLicense,
        MedicalAidName = includeMedicalAid ? registration.MedicalAidName : null,
        MedicalAidNumber = includeMedicalAid ? registration.MedicalAidNumber : null,
        Status = registration.Status,
        CategoryName = registration.Category?.Name,
        EventName = registration.Category?.Event?.Title,
        EventDate = registration.Category?.Event?.Date,
        ParticipantName = registration.Participant?.User is { } user ? $"{user.FirstName} {user.LastName}" : null
    };

    public static ResultResponseDto ToResponse(Result result) => new()
    {
        Id = result.Id,
        RegistrationId = result.RegistrationId,
        CategoryId = result.Registration?.CategoryId ?? 0,
        GunTime = result.GunTime,
        NetTime = result.NetTime,
        OverallPosition = result.OverallPosition,
        CategoryPosition = result.CategoryPosition,
        Status = result.Status,
        CategoryName = result.Registration?.Category?.Name,
        EventName = result.Registration?.Category?.Event?.Title,
        EventDate = result.Registration?.Category?.Event?.Date,
        RegistrationEventId = result.Registration?.Category?.EventId,
        FinishTime = result.NetTime ?? result.GunTime
    };

    public static ClubResponseDto ToResponse(Club club) => new()
    {
        Id = club.Id,
        Name = club.Name,
        RegistrationNumber = club.RegistrationNumber,
        ContactEmail = club.ContactEmail,
        ContactNumber = club.ContactNumber,
        CreatedAt = club.CreatedAt
    };

    public static DisciplineResponseDto ToResponse(Discipline discipline) => new()
    {
        Id = discipline.Id,
        Name = discipline.Name,
        Description = discipline.Description
    };

    public static MembershipResponseDto ToResponse(ClubMembership membership) => new()
    {
        Id = membership.Id,
        ClubId = membership.ClubId,
        DisciplineId = membership.DisciplineId,
        MembershipNumber = membership.MembershipNumber,
        JoinedAt = membership.JoinedAt,
        Status = membership.Status
    };
}
