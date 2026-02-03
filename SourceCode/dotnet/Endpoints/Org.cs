using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

public static class OrgEndpoints
{
    public static void MapOrgEndpoints(this IEndpointRouteBuilder app)
    {
        var orgs = app.MapGroup("/orgs");

        // Map endpoints

        orgs.MapGet("/get/{orgId}", getOrg);
        orgs.MapPost("/create/{name}", createOrg);
        orgs.MapDelete("/delete", deleteOrg);
     
    }
  

  private static async Task<IResult> getOrg(int orgId, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        if (!currentUser.isRegistered()) return Results.BadRequest("Current API user not signed up");

        // Reject if the current user is not in the same org
        if (currentUser.OrgID != orgId) return Results.Forbid();

        Organisation? organisation = await db.Organisations.FindAsync(orgId);
        
        return Results.Ok(organisation);
        
    }


  private static async Task<IResult> createOrg(string name, AppDbContext db, IHttpContextAccessor httpAccessor)
    {

        if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest("No name provided");

        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // If current user is in an org, reject
        if (currentUser.OrgID != 0) return Results.Forbid();


        User? user = await db.UserAccessLevels.FindAsync(currentUser.UserID);
        if (user == null) return Results.NotFound("Current user not found in DB");

        // Create new org
        Organisation org = new Organisation();
        org.OrgName = name;

        // Add to db
        db.Organisations.Add(org);
        await db.SaveChangesAsync();

        // Update user
        user.OrgID = org.OrgID;
        user.AccessLevel = 4;
        await db.SaveChangesAsync();


        return Results.Created();
    }



    private static async Task<IResult> deleteOrg( AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync(); 

        // Cannot delete the unnasigned org group
        if (currentUser.isRegToOrg(0)) return Results.Forbid();

        // If current user is not root, reject
        if (!currentUser.hasAccessLevel(4)) return Results.Forbid();

        Organisation? org = await db.Organisations.FindAsync(currentUser.OrgID);
        if (org == null) return Results.NotFound("Organisation not found");

        // Get a list of all users
        List<User> usersInOrg = await db.UserAccessLevels.Where(u => u.OrgID == currentUser.OrgID).ToListAsync();

        // Unregister them from the org
        foreach (User user in usersInOrg)
        {
            user.OrgID = 0;
            user.AccessLevel = 1;
        }
        await db.SaveChangesAsync();



        // Remove ALL entities referencing this org
        List<Policy> policiesInOrg = await db.Policies.Where(u => u.OrgID == currentUser.OrgID).ToListAsync();
        db.Policies.RemoveRange(policiesInOrg);
        await db.SaveChangesAsync();

        List<OrgJoinCode> orgJoinCodes = await db.OrgJoinCodes.Where(u => u.OrgID == currentUser.OrgID).ToListAsync();
        db.OrgJoinCodes.RemoveRange(orgJoinCodes);
        await db.SaveChangesAsync();

        List<Geofence> geofences = await db.Geofences.Where(u => u.OrgID == currentUser.OrgID).ToListAsync();
        db.Geofences.RemoveRange(geofences);
        await db.SaveChangesAsync();

        List<DevicePolicyStatus> statuses = await db.DevicePolicyStatus.Where(u => u.OrgID == currentUser.OrgID).ToListAsync();
        db.DevicePolicyStatus.RemoveRange(statuses);
        await db.SaveChangesAsync();

        List<DeviceJoinCode> deviceJoinCodes = await db.DeviceJoinCodes.Where(u => u.OrgID == currentUser.OrgID).ToListAsync();
        db.DeviceJoinCodes.RemoveRange(deviceJoinCodes);
        await db.SaveChangesAsync();

        List<DeviceGroup> groups = await db.DeviceGroups.Where(u => u.OrgID == currentUser.OrgID).ToListAsync();
        db.DeviceGroups.RemoveRange(groups);
        await db.SaveChangesAsync();

        List<Device> devices = await db.Devices.Where(u => u.OrgID == currentUser.OrgID).ToListAsync();
        db.Devices.RemoveRange(devices);
        await db.SaveChangesAsync();



        db.Organisations.Remove(org);
        await db.SaveChangesAsync();

        return Results.Ok("Organisation deleted ");
    }

}