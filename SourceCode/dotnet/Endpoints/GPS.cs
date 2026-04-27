using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public static class GPSEndpoints
{
    public static void MapGPSEndpoints(this IEndpointRouteBuilder app)
    {
        var fences = app.MapGroup("/gps");

        // Map endpoints
        fences.MapPost("/update/{deviceId}", updateGPS);
    
    }


    // Methods for endpoints
    private static async Task<IResult> updateGPS( InboundMessage payload, AppDbContext db, IHttpContextAccessor httpAccessor)
    {






        return Results.Ok();
        
    }






}