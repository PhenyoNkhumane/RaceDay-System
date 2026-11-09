using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public static class UserMapper
{
    public static UserResponseDto ToResponse(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role,
        IdNumberOrPassport = user.IdNumberOrPassport,
        AsaPermanentLicenseNumber = user.AsaPermanentLicenseNumber,
        CreatedAt = user.CreatedAt,
        ProfilePictureUrl = user.ProfilePictureUrl
    };
}
