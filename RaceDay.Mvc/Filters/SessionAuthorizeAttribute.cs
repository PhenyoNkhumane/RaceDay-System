using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute(string? requiredRole = null) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (session.GetInt32("UserId") is null || string.IsNullOrWhiteSpace(session.GetString("Role")))
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
            return;
        }

        if (requiredRole is not null && session.GetString("Role") != requiredRole)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
