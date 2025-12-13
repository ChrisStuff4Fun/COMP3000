using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public static class GPSEndpoints
{
    public static void MapGPSEndpoints(this IEndpointRouteBuilder app)
    {
        var fences = app.MapGroup("/gps");

        // Map endpoints
        fences.MapGet("/update/{deviceId}", updateGPS);
    
    }


    // Methods for endpoints
    private static async Task<IResult> updateGPS( int deviceId, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        return Results.Ok();
        
    }







}