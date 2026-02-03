using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

public static class JoinCodeEndpoints
{
    public static void MapCodeEndpoints(this IEndpointRouteBuilder app)
    {
        var codes = app.MapGroup("/joincodes");

        // Map endpoints
        codes.MapPost("/createdevicecode/{duration}/", createDeviceJoinCode);
        codes.MapPost("/createusercode/{duration}/", createUserJoinCode);
        codes.MapDelete("/purgedevicecodes", purgeDeviceCodes);
        codes.MapDelete("/purgeusercodes", purgeUserCodes);
        codes.MapGet("/getusercodes", getUserCodesByOrg);
        codes.MapGet("/getdevicecodes", getDeviceCodesByOrg);
    }
  
  
  private static async Task<IResult> createUserJoinCode(int duration, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // If current user is not admin or root, reject
        if (!currentUser.hasAccessLevel(3)) return Results.Forbid();


        // Create new join code obj
        OrgJoinCode joinCode = new OrgJoinCode();
        joinCode.OrgID = currentUser.OrgID;
        joinCode.ExpiryDate = DateTime.UtcNow.Add(TimeSpan.FromHours(duration));
        joinCode.Code = JoinCodeGenerator.generateJoinCode();
        joinCode.IsUsed = false;

        // Add to db
        db.OrgJoinCodes.Add(joinCode);
        await db.SaveChangesAsync();

        return Results.Created();
    }



      private static async Task<IResult> createDeviceJoinCode(int duration, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync(); 

        // If current user is not admin or root, reject
        if (!currentUser.hasAccessLevel(3)) return Results.Forbid();


        // Create new join code obj
        DeviceJoinCode joinCode = new DeviceJoinCode();
        joinCode.OrgID = currentUser.OrgID;
        joinCode.ExpiryDate = DateTime.UtcNow.Add(TimeSpan.FromHours(duration));
        joinCode.Code = JoinCodeGenerator.generateJoinCode();
        joinCode.IsUsed = false;

        // Add to db
        db.DeviceJoinCodes.Add(joinCode);
        await db.SaveChangesAsync();

        return Results.Created();
    }

    private static async Task<IResult> purgeDeviceCodes(AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // If current user is not admin or root, reject
        if (!currentUser.hasAccessLevel(3)) return Results.Forbid();

        // Get list of codes to delete
        List<DeviceJoinCode> codesToDelete = await db.DeviceJoinCodes.Where(j => j.OrgID == currentUser.OrgID && (j.IsUsed || j.ExpiryDate < DateTime.UtcNow)).ToListAsync();

        if (codesToDelete.Count == 0) return Results.Ok("0 codes deleted");

        db.DeviceJoinCodes.RemoveRange(codesToDelete);
        await db.SaveChangesAsync();

        return Results.Ok($"{codesToDelete.Count} codes deleted");

    }


        private static async Task<IResult> purgeUserCodes(AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // If current user is not admin or root, reject
        if (!currentUser.hasAccessLevel(3)) return Results.Forbid();

        // Get list of codes to delete
        List<OrgJoinCode> codesToDelete = await db.OrgJoinCodes.Where(j => j.OrgID == currentUser.OrgID && (j.IsUsed || j.ExpiryDate < DateTime.UtcNow)).ToListAsync();

        if (codesToDelete.Count == 0) return Results.Ok("0 codes deleted");

        db.OrgJoinCodes.RemoveRange(codesToDelete);
        await db.SaveChangesAsync();

        return Results.Ok($"{codesToDelete.Count} codes deleted");

    }


    private static async Task<IResult> getUserCodesByOrg( AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or the org, or if they are not level 2 or higher
        if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(2)) return Results.Forbid();

        List<OrgJoinCode> codes = await db.OrgJoinCodes.Where(c => c.OrgID == currentUser.OrgID).ToListAsync();
        return codes.Any() ? Results.Ok(codes) : Results.NotFound();
        
    }

    private static async Task<IResult> getDeviceCodesByOrg( AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or the org, or if they are not level 2 or higher
        if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(2)) return Results.Forbid();

        List<DeviceJoinCode> codes = await db.DeviceJoinCodes.Where(c => c.OrgID == currentUser.OrgID).ToListAsync();
        return codes.Any() ? Results.Ok(codes) : Results.NotFound();
        
    }




}