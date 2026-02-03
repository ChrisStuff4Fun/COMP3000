using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.EntityFrameworkCore;

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
    private static async Task<IResult> issueJwt([FromBody] GoogleTokenRequest requestBody, HttpRequest request, HttpResponse response, AppDbContext db)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {

            string token = requestBody.Token;
            payload = await GoogleJsonWebSignature.ValidateAsync(token);
        }
        catch
        {
            return Results.Unauthorized();
        }

        string googleSub = payload.Subject;




        // Set auth cookie
        response.Cookies.Append(
        "auth",
        googleSub,
        new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Lax,  // May switch to strict in the future if it works
            Expires  = DateTimeOffset.UtcNow.AddDays(7)
        });
    
        

        return Results.Ok();
    }

    private static async Task<IResult> authMe(HttpRequest request, AppDbContext db)
    {
        // make sure current user is definitely authed with google
        
        if (!request.Cookies.TryGetValue("auth", out var googleSub) || string.IsNullOrWhiteSpace(googleSub))
        {
            return Results.Ok(new { authenticated = false });
        }

        // Check if user exists in db
        User? user = await db.UserAccessLevels.FirstOrDefaultAsync(u => u.GoogleSub == googleSub);

        if (user != null)
        {
            return Results.Ok(new
            {
                authenticated = true,
                registered = true,
                username = user.Name
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


     private static async Task<IResult> logout(HttpResponse response)
    {
        response.Cookies.Delete("auth");
        return Results.Ok();
    }





}