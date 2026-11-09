using Microsoft.AspNetCore.Authorization;

namespace RaceDay.Api.Services;

public class RequireRoleAttribute : AuthorizeAttribute
{
    public RequireRoleAttribute(params string[] roles)
    {
        if (roles.Length > 0)
        {
            Roles = string.Join(",", roles);
        }
    }
}

/// <summary>Any logged-in user, regardless of role.</summary>
public sealed class RequireLoginAttribute : RequireRoleAttribute
{
}
