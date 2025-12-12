using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

public static class KeyEndpoints
{
    public static void MapKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var keys = app.MapGroup("/keys");

        // Map endpoints
        keys.MapGet("/public", servePublicKey);
        keys.MapGet("/register/{inboundMessage:string}", registerDeviceKeys);

        // DO NOT UNCOMMENT WITHOUT SERIOUS CONSIDERATION
        keys.MapGet("/generateserverkeyset", generateKeys)

    }



    // Methods for endpoints
    private static async Task<IResult> servePublicKey( AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        return Results.Ok();
    }



    private static async Task<IResult> registerDeviceKeys( String inboundMessage, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        return Results.Ok();
    }

    

    private static async Task<IResult> generateKeys()
    {
        return Results.Ok();
    }


}
