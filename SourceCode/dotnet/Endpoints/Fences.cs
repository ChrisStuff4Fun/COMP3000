using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public static class FenceEndpoints
{
    public static void MapFenceEndpoints(this IEndpointRouteBuilder app)
    {
        var fences = app.MapGroup("/geofence");

        // Map endpoints
        fences.MapGet("/geofences", getFencesByOrg);
        fences.MapGet("/delete/{fenceId}", deleteFence);
        fences.MapPost("/create/{name}", createFence);
        fences.MapPut("/update/{fenceId}/{newName}", updateFence);
    }


    // Methods for endpoints
    private static async Task<IResult> getFencesByOrg( AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or the org, or if they are not level 1 or higher
        if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(1)) return Results.Forbid();

        List<Geofence> fences = await db.Geofences.Where(g => g.OrgID == currentUser.OrgID).ToListAsync();
        return fences.Any() ? Results.Ok(fences) : Results.NotFound();
        
    }


    private static async Task<IResult> deleteFence(int fenceId, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // Only admin and root can delete fences
        if (currentUser.AccessLevel < 3) return Results.Forbid();

        // Get fence to delete
        Geofence? fence = await db.Geofences.FindAsync(fenceId);

        // Check if fence exisits
        if (fence == null) return Results.BadRequest("Geofence does not exist.");

        // Prevent deletion of other orgs geofences
        if (fence.OrgID != currentUser.OrgID) return Results.Forbid();

        // Remove fence and save
        db.Geofences.Remove(fence);
        await db.SaveChangesAsync();

        return Results.Ok();
    }


    private static async Task<IResult> createFence(string name, [FromBody] string newFence, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // Only admin and root can create fences
        if (currentUser.AccessLevel < 3) return Results.Forbid();


        Geofence fence = new Geofence();
        // Force ordId to be that of the user creating it 
        fence.OrgID        = currentUser.OrgID;
        fence.GeoJSON      = newFence;
        fence.GeofenceName = name;
        
        db.Geofences.Add(fence);
        await db.SaveChangesAsync();

        return Results.Created();
    }

    private static async Task<IResult> updateFence(int fenceId, string newName, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();

        // Get fence to update
        Geofence? fence = await db.Geofences.FindAsync(fenceId);
        if (fence == null) return Results.Conflict("Fence does not exists");

        // Reject if current user is in different org or is not an admin
        if (fence.OrgID != currentUser.OrgID || currentUser.AccessLevel >= 3) return Results.Forbid();

        // Edit current fence name
        fence.GeofenceName = newName;

        await db.SaveChangesAsync();
        return Results.Ok();
        
    }





}