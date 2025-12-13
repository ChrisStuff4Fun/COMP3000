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
        devices.MapGet("/delete/{deviceId:int}", deleteDevice);
        devices.MapPut("/create", createDevice);
        devices.MapPut("/update/{deviceId:int}", updateDevice);
    }



    // Methods for endpoints
    private static async Task<IResult> getDevicesByOrg( AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!await currentUser.validateGoogleTokenAsync()) return Results.Unauthorized();
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
        if (!await currentUser.validateGoogleTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // Only admin and root can delete policies
        if (currentUser.AccessLevel < 3) return Results.Forbid();

        // Get device to delete
        Device? device = await db.Devices.FindAsync(deviceId);

        // Check if device exisits
        if (device == null) return Results.BadRequest("Policy does not exist.");

        // Prevent deletion of other orgs devices
        if (device.OrgID != currentUser.OrgID) return Results.Forbid();

        // Remove device and save
        db.Devices.Remove(device);
        await db.SaveChangesAsync();

        return Results.Ok();
    }


    private static async Task<IResult> createDevice([FromBody] Policy newPolicyIn, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!await currentUser.validateGoogleTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // Only admin and root can create policies
        if (currentUser.AccessLevel < 3) return Results.Forbid();

        // Set policy Id to impossible value so that db server autofills it
        newPolicyIn.PolicyID = 0;
        // Force ordId to be that of the user creating it 
        newPolicyIn.OrgID    = currentUser.OrgID;

        db.Policies.Add(newPolicyIn);
        await db.SaveChangesAsync();

        return Results.Created();
    }

    private static async Task<IResult> updateDevice(int policyId, [FromBody] Policy newPolicy, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!await currentUser.validateGoogleTokenAsync()) return Results.Unauthorized();

        // Get policy to update
        Policy? policy = await db.Policies.FindAsync(policyId);
        if (policy == null) return Results.Conflict("Policy does not exists");

        // Reject if current user is in different org or is not an admin
        if (policy.OrgID != currentUser.OrgID || currentUser.AccessLevel >= 3) return Results.Forbid();

        // Edit current policy
        policy.PolicyName            = newPolicy.PolicyName;
        policy.GeofenceID            = newPolicy.GeofenceID;
        policy.DeviceGroupID         = newPolicy.DeviceGroupID;
        policy.AlertOnLeaveRule      = newPolicy.AlertOnLeaveRule;
        policy.AlertOnEnterRule      = newPolicy.AlertOnEnterRule;
        policy.TrackInsideFenceRule  = newPolicy.TrackInsideFenceRule;
        policy.TrackOutsideFenceRule = newPolicy.TrackOutsideFenceRule;


        await db.SaveChangesAsync();
        return Results.Ok();
        
    }


}
