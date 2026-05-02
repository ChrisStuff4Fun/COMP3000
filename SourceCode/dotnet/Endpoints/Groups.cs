using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var groups = app.MapGroup("/group");

        // Map endpoints
        groups.MapGet("/groups", getGroupsByOrg);
        groups.MapDelete("/delete/{groupId}", deleteGroup);
        groups.MapPut("/create", createGroup);
        groups.MapPut("/update/{groupId}", updateGroup);
        groups.MapPut("/{groupId}/adddevice/{deviceID}", addDeviceToGroup);
        groups.MapPut("/{groupId}/removedevice/{deviceID}", removeDeviceFromGroup);
        groups.MapGet("/{groupId}/devices", getDevicesInGroup);
    }


    // Methods for endpoints
    private static async Task<IResult> getDevicesInGroup(int groupId, AppDbContext db, IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or the org, or if they are not level 2 or higher
        if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(2)) return Results.Problem("Forbidden", statusCode: 403);

        DeviceGroup? group = await db.DeviceGroups.FindAsync(groupId);
        if (group == null) return Results.NotFound();

        if (group.OrgID != currentUser.OrgID)
            return Results.Problem("Forbidden", statusCode: 403);

        List<Device> devices = await db.Devices_DeviceGroup_Link
            .Where(l => l.DeviceGroupID == groupId)
            .Join(db.Devices,
                link => link.DeviceID,
                device => device.DeviceID,
                (link, device) => device)
            .ToListAsync();

        return Results.Ok(devices);
    }


    private static async Task<IResult> getGroupsByOrg( AppDbContext db, IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or the org, or if they are not level 1 or higher
        if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(1)) return Results.Problem("Forbidden", statusCode: 403);

        List<DeviceGroup> groups = await db.DeviceGroups.Where(g => g.OrgID == currentUser.OrgID).ToListAsync();
        return groups.Any() ? Results.Ok(groups) : Results.NotFound();
        
    }


    private static async Task<IResult> deleteGroup(int groupId, AppDbContext db, IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // Only admin and root can delete policies
        if (currentUser.AccessLevel < 3) return Results.Problem("Forbidden", statusCode: 403);

        // Get group to delete
        DeviceGroup? group = await db.DeviceGroups.FindAsync(groupId);

        // Check if group exisits
        if (group == null) return Results.BadRequest("Group does not exist.");

        // Prevent deletion of other orgs devices
        if (group.OrgID != currentUser.OrgID) return Results.Problem("Forbidden", statusCode: 403);

        // Remove device and save
        db.DeviceGroups.Remove(group);
        await db.SaveChangesAsync();

        return Results.Ok();
    }


    private static async Task<IResult> createGroup([FromBody] DeviceGroup newGroup, AppDbContext db, IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // Only admin and root can create groups
        if (currentUser.AccessLevel < 3) return Results.Problem("Forbidden", statusCode: 403);

        // Set group Id to impossible value so that db server autofills it
        newGroup.DeviceGroupID = 0;
        // Force ordId to be that of the user creating it 
        newGroup.OrgID    = currentUser.OrgID;

        db.DeviceGroups.Add(newGroup);
        await db.SaveChangesAsync();

        return Results.Created();
    }

    private static async Task<IResult> updateGroup(int groupId, [FromBody] DeviceGroup newGroup, AppDbContext db, IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();

        // Get group to update
        DeviceGroup? group = await db.DeviceGroups.FindAsync(groupId);
        if (group == null) return Results.Conflict("Group does not exists");

        // Reject if current user is in different org or is not an admin
        if (group.OrgID != currentUser.OrgID || currentUser.hasAccessLevel(3)) return Results.Problem("Forbidden", statusCode: 403);

        // Edit current group
        group.GroupName    = newGroup.GroupName;
        group.GPSAccuracy  = newGroup.GPSAccuracy;

        await db.SaveChangesAsync();
        return Results.Ok();
        
    }

    private static async Task<IResult> addDeviceToGroup(int groupId, int deviceId, AppDbContext db, IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();

        await currentUser.getUserFromDBAsync();

        // Get group and device
        DeviceGroup? group = await db.DeviceGroups.FindAsync(groupId);
        if (group == null) return Results.Conflict("Group does not exist");

        Device? device = await db.Devices.FindAsync(deviceId);
        if (device == null) return Results.Conflict("Device does not exist");

        // Reject if current user is in different org or is not an admin
        if (group.OrgID != currentUser.OrgID || !currentUser.hasAccessLevel(3) || device.OrgID != currentUser.OrgID) return Results.Problem("Forbidden", statusCode: 403);

        // Check if device is already in the group
        DeviceDeviceGroupLink? link = await db.Devices_DeviceGroup_Link.SingleOrDefaultAsync(l => l.DeviceGroupID == groupId && l.DeviceID == deviceId);
        if (link != null) return Results.BadRequest("Link already exists");

        DeviceDeviceGroupLink newLink = new DeviceDeviceGroupLink();
        newLink.DeviceGroupID = groupId;
        newLink.DeviceID      = deviceId;

        db.Devices_DeviceGroup_Link.Add(newLink);
        
        await db.SaveChangesAsync();
        return Results.Created();
        
    }


      private static async Task<IResult> removeDeviceFromGroup(int groupId, int deviceId, AppDbContext db, IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();

        await currentUser.getUserFromDBAsync();


        // Get group and device
        DeviceGroup? group = await db.DeviceGroups.FindAsync(groupId);
        if (group == null) return Results.Conflict("Group does not exist");

        Device? device = await db.Devices.FindAsync(deviceId);
        if (device == null) return Results.Conflict("Device does not exist");

        // Reject if current user is in different org or is not an admin
        if (group.OrgID != currentUser.OrgID || !currentUser.hasAccessLevel(3) || device.OrgID != currentUser.OrgID) return Results.Problem("Forbidden", statusCode: 403);

        // Find link 
        DeviceDeviceGroupLink? link = await db.Devices_DeviceGroup_Link.SingleOrDefaultAsync(l => l.DeviceGroupID == groupId && l.DeviceID == deviceId);
        // Check if it exists
        if (link == null) return Results.NotFound("Link not found");

        db.Devices_DeviceGroup_Link.Remove(link);
        
        await db.SaveChangesAsync();
        return Results.Created();
        
    }





}