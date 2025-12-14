using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.SwaggerGen;

public static class AuthEndpoints
{

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var fences = app.MapGroup("/auth");

        // Map endpoints
        fences.MapPost("/google", issueJwt);
    
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
    catch (Exception e)
    {
        return Results.Json(new {success = false, error = e.Message}, statusCode: 401);
    }

    string googleSub = payload.Subject;



    try
    {
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
    }
    catch (Exception e)
    {
        return Results.Json(new {success = false, error = e.Message}, statusCode: 401);
    }
    

    return Results.Ok();
    }



}