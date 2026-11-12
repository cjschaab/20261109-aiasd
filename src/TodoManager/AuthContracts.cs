using System.ComponentModel.DataAnnotations;

namespace TodoManager;

/// <summary>Credentials for a nonpersistent cookie sign-in.</summary>
public sealed record LoginRequest
{
    [Required, StringLength(256)]
    public required string UserName { get; init; }

    [Required, StringLength(1024)]
    public required string Password { get; init; }
}

/// <summary>Login availability, current user, and request-verification token.</summary>
public sealed record AuthSessionResponse(bool LoginEnabled, bool IsAuthenticated, string? UserName, string? CsrfToken);
