using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.EntityFrameworkCore;
//using Microsoft.AspNetCore.DataProtection;

public static class AuthEndpoints
{

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth");

        // Map endpoints
        auth.MapPost("/google", issueJwt);
        auth.MapGet("/me",      authMe);
        auth.MapPost("/logout", logout);
    
    }

        private class GoogleTokenRequest
        {
            public required string Token { get; set; }
        }


    // Methods for endpoints
    private static async Task<IResult> issueJwt([FromBody] GoogleTokenRequest requestBody, HttpRequest request, HttpResponse response, AppDbContext db ) //IDataProtectionProvider dataProtectionProvider
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(requestBody.Token);
        }
        catch
        {
            return Results.Unauthorized();
        }

        string googleSub = payload.Subject;

        // Use Data Protection to sign cookie
        //var protector = dataProtectionProvider.CreateProtector("AuthCookieProtector");
        //string protectedValue = protector.Protect(googleSub);

        response.Cookies.Append(
            "auth",
            googleSub,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

        return Results.Ok();
    }

    private static async Task<IResult> authMe(HttpRequest request, AppDbContext db) //  IDataProtectionProvider dataProtectionProvider
    {
        if (!request.Cookies.TryGetValue("auth", out var googleSub) || string.IsNullOrWhiteSpace(googleSub))
            return Results.Ok(new { authenticated = false });

        try
        {
            //var protector = dataProtectionProvider.CreateProtector("AuthCookieProtector");
            //string googleSub = protector.Unprotect(protectedValue);

            User? user = await db.UserAccessLevels.FirstOrDefaultAsync(u => u.GoogleSub == googleSub);

            if (user != null)
            {
                Organisation? organisation = await db.Organisations.FindAsync(user.OrgID);

                return Results.Ok(new
                {
                    authenticated = true,
                    registered = true,
                    username = user.Name,
                    orgId = user.OrgID,
                    orgName = organisation?.OrgName,
                    accessLevel = user.AccessLevel
                });
            }
            else
            {
                return Results.Ok(new
                {
                    authenticated = true,
                    registered = false
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