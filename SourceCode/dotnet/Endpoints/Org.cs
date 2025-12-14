using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

public static class OrgEndpoints
{
    public static void MapOrgEndpoints(this IEndpointRouteBuilder app)
    {
        var codes = app.MapGroup("/orgs");

        // Map endpoints
        codes.MapGet("/create/{name}", createOrg);
        codes.MapGet("/delete", deleteOrg);
     
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


        List<User> usersInOrg = await db.UserAccessLevels.Where(u => u.OrgID == currentUser.OrgID).ToListAsync();

        foreach (User user in usersInOrg)
        {
            user.OrgID = 0;
            user.AccessLevel = 1;
        }

        db.Organisations.Remove(org);
        await db.SaveChangesAsync();

        return Results.Ok("Organisation deleted ");
    }

}