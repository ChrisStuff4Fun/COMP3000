using Google.Apis.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth");
        auth.MapPost("/google", issueJwt);
        auth.MapGet("/me",      authMe);
        auth.MapPost("/logout", logout);
    }

    private class GoogleTokenRequest
    {
        public required string Token { get; set; }
    }

    private static async Task<IResult> issueJwt([FromBody] GoogleTokenRequest requestBody, HttpResponse response, AppDbContext db, IDataProtectionProvider dataProtectionProvider)
    {
        // Validate Google token
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(requestBody.Token);
        }
        catch
        {
            return Results.Unauthorized();
        }

        // Protect the googleSub before storing in cookie
        var protector = dataProtectionProvider.CreateProtector("AuthCookieProtector");
        string protectedValue = protector.Protect(payload.Subject);

        response.Cookies.Append(
            "auth",
            protectedValue,
            new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.Lax,
                Expires  = DateTimeOffset.UtcNow.AddDays(7)
            });

        return Results.Ok();
    }

    private static async Task<IResult> authMe(HttpRequest request, AppDbContext db, IDataProtectionProvider dataProtectionProvider)
    {
        if (!request.Cookies.TryGetValue("auth", out var protectedValue) || string.IsNullOrWhiteSpace(protectedValue))
            return Results.Ok(new { authenticated = false });

        string googleSub;
        try
        {
            var protector = dataProtectionProvider.CreateProtector("AuthCookieProtector");
            googleSub = protector.Unprotect(protectedValue);
        }
        catch
        {
            // Cookie was tampered with or keys have rotated
            return Results.Ok(new { authenticated = false });
        }

        try
        {
            User? user = await db.UserAccessLevels.FirstOrDefaultAsync(u => u.GoogleSub == googleSub);
            if (user != null)
            {
                Organisation? organisation = await db.Organisations.FindAsync(user.OrgID);
                return Results.Ok(new
                {
                    authenticated = true,
                    registered    = true,
                    username      = user.Name,
                    orgId         = user.OrgID,
                    orgName       = organisation?.OrgName,
                    accessLevel   = user.AccessLevel
                });
            }
            else
            {
                return Results.Ok(new
                {
                    authenticated = true,
                    registered    = false
                });
            }
        }
        catch
        {
            return Results.Ok(new { authenticated = false });
        }
    }

    private static IResult logout(HttpResponse response)
    {
        response.Cookies.Delete("auth");
        return Results.Ok();
    }
}