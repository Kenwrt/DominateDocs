

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace DominateDocsData.Helpers;

public static class AuthStateExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        // Common claim types depending on auth provider:
        // - ASP.NET Identity: ClaimTypes.NameIdentifier
        // - JWT / OIDC: "sub"
        // - Some providers: "oid" (Azure AD object id), "user_id"
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("oid")
            ?? user.FindFirstValue("user_id");
    }

    public static string? GetDisplayName(this ClaimsPrincipal user)
    {
        // Best: explicit OIDC display name
        var name = user.FindFirstValue("name");
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        // Next: build from first + last
        var given = user.FindFirstValue(ClaimTypes.GivenName) ?? user.FindFirstValue("given_name");
        var family = user.FindFirstValue(ClaimTypes.Surname) ?? user.FindFirstValue("family_name");

        var full = string.Join(" ", new[] { given, family }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(full))
            return full;

        // Fallbacks (may be login/email depending on provider)
        return user.FindFirstValue(ClaimTypes.Name)
            ?? user.Identity?.Name;
    }
}

