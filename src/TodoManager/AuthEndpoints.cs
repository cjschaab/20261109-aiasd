using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace TodoManager;

public static class AuthEndpoints
{
    public static void MapAuth(this WebApplication app, bool loginEnabled)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");
        group.MapGet("/session", (HttpContext context, IAntiforgery antiforgery) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return TypedResults.Ok(new AuthSessionResponse(loginEnabled,
                loginEnabled && context.User.Identity?.IsAuthenticated == true,
                loginEnabled ? context.User.Identity?.Name : null,
                loginEnabled ? antiforgery.GetAndStoreTokens(context).RequestToken : null));
        }).WithName("GetAuthSession").WithSummary("Get login state and an antiforgery token.");

        if (!loginEnabled)
            return;

        group.MapPost("/login", async Task<Results<NoContent, ProblemHttpResult>> (
            LoginRequest request, SignInManager<ApplicationUser> signIn) =>
        {
            var result = await signIn.PasswordSignInAsync(
                request.UserName, request.Password, isPersistent: false, lockoutOnFailure: true);
            return result.Succeeded
                ? TypedResults.NoContent()
                : TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized,
                    detail: "Invalid username or password.");
        }).RequireRateLimiting("login")
            .WithName("Login").WithSummary("Sign in with an existing username and password.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/logout", async (SignInManager<ApplicationUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return TypedResults.NoContent();
        }).RequireAuthorization().WithName("Logout").WithSummary("Clear the current browser's authentication cookie.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
