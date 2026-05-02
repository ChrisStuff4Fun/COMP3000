using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this IEndpointRouteBuilder app)
    {
        var status = app.MapGroup("/status");

        // Map endpoints
        status.MapGet("/status/get", getStatusByOrg);
        status.MapDelete("/alerts/clear", clearAlerts);
        status.MapGet("/alerts/get", getAlerts);

    }



    // Methods for endpoints
    private static async Task<IResult> getStatusByOrg( AppDbContext db, IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or the org, or if they are not level 2 or higher
        if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(1)) return Results.Problem("Forbidden", statusCode: 403);

        List<DevicePolicyStatus> statuses = await db.DevicePolicyStatus.Where(d => d.OrgID == currentUser.OrgID).ToListAsync();

        List<DeviceStatusOut> deviceStatusOuts = new List<DeviceStatusOut>();

        foreach (DevicePolicyStatus status in statuses)
        {
            Device? device = await db.Devices.FindAsync(status.DeviceID);
            Policy? policy = await db.Policies.FindAsync(status.PolicyID);

            if (device == null || policy == null) continue;

            deviceStatusOuts.Add(new DeviceStatusOut
            {
                DeviceName = device.DeviceName,
                PolicyName = policy.PolicyName,
                IsInsideFence = status.IsInsideFence,
                LastUpdated = status.LastUpdated

            });
        }

        return Results.Ok(deviceStatusOuts.OrderByDescending(d => d.PolicyName));
        
    }


    private static async Task<IResult> getAlerts( AppDbContext db, IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or the org, or if they are not level 2 or higher
        if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(1)) return Results.Problem("Forbidden", statusCode: 403);

        List<DevicePolicyStatus> statuses = await db.DevicePolicyStatus.Where(d => d.OrgID == currentUser.OrgID).ToListAsync();

        List<AlertOut> alertOuts = new List<AlertOut>();

        foreach (DevicePolicyStatus status in statuses)
        {
            Device? device = await db.Devices.FindAsync(status.DeviceID);
            Policy? policy = await db.Policies.FindAsync(status.PolicyID);

            if (device == null || policy == null) continue;

            Geofence? geofence = await db.Geofences.FindAsync(policy.GeofenceID);

            if (geofence == null) continue;

            alertOuts.Add(new AlertOut
            {
                DeviceName = device.DeviceName,
                GeofenceName = geofence.GeofenceName,
                AlertOnEnterTriggered = status.AlertOnEnterTriggered,
                AlertOnLeaveTriggered = status.AlertOnLeaveTriggered,
                LastUpdated = status.LastUpdated

            });
        }

        // return to frontend by soonest first
        return Results.Ok(alertOuts.OrderByDescending(a => a.LastUpdated));
        
    }


    private static async Task<IResult> clearAlerts( AppDbContext db, IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
        // Reject if user isnt authed by google
        if (!currentUser.validateToken()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or the org, or if they are not level 2 or higher
        if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(1)) return Results.Problem("Forbidden", statusCode: 403);

        List<DevicePolicyStatus> statuses = await db.DevicePolicyStatus.Where(d => d.OrgID == currentUser.OrgID).ToListAsync();

        foreach (DevicePolicyStatus status in statuses)
        {
            status.AlertOnEnterTriggered = false;
            status.AlertOnLeaveTriggered = false;
        }

        await db.SaveChangesAsync();

        return Results.Ok();
        
    }


   

}
