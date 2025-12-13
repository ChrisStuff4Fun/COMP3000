using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        var devices = app.MapGroup("/device");

        // Map endpoints
        devices.MapGet("/devices", getDevicesByOrg);
        devices.MapGet("/release/{deviceId}", deleteDevice);
        devices.MapPut("/update/{deviceId}/{newName}", updateDevice);
    }



    // Methods for endpoints
    private static async Task<IResult> getDevicesByOrg( AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or the org, or if they are not level 2 or higher
        if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(2)) return Results.Forbid();

        List<Device> devices = await db.Devices.Where(d => d.OrgID == currentUser.OrgID).ToListAsync();
        return devices.Any() ? Results.Ok(devices) : Results.NotFound();
        
    }


    private static async Task<IResult> deleteDevice(int deviceId, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // Only admin and root can delete policies
        if (currentUser.AccessLevel < 3) return Results.Forbid();

        // Get device to delete
        Device? device = await db.Devices.FindAsync(deviceId);

        // Check if device exisits
        if (device == null) return Results.BadRequest("Device does not exist.");

        // Prevent deletion of other orgs devices
        if (device.OrgID != currentUser.OrgID) return Results.Forbid();

        // Remove device and save
        db.Devices.Remove(device);
        await db.SaveChangesAsync();

        return Results.Ok();
    }

    private static async Task<IResult> updateDevice(int deviceId, string newName, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();

        // Get device to update
        Device? device = await db.Devices.FindAsync(deviceId);
        if (device == null) return Results.Conflict("Device does not exist");

        // Reject if current user is in different org or is not an admin
        if (device.OrgID != currentUser.OrgID || currentUser.AccessLevel >= 3) return Results.Forbid();

        // Edit device name
        device.DeviceName = newName;

        await db.SaveChangesAsync();
        return Results.Ok();
        
    }


}
