using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

public static class AuthEndpoints
{

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var fences = app.MapGroup("/auth");

        // Map endpoints
        fences.MapGet("/google", issueJwt);
    
    }


    // Methods for endpoints
    private static async Task<IResult> issueJwt([FromBody] string idToken, HttpRequest request, HttpResponse response, AppDbContext db)
    {
        GoogleJsonWebSignature.Payload payload;
    try
    {
        payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
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



}