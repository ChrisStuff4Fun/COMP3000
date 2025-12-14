using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/user");

        // Map endpoints
        users.MapGet("/users", getUsersByOrg);
        users.MapGet("/register/{joinCode}", regUserToOrg);
        users.MapGet("/release/{userId}", releaseUserFromOrg);
        users.MapGet("/delete", deleteUser);
        users.MapGet("/create/{name}", createUser);
        users.MapPut("/update/{userId}/{newAL}", updateUserAccessLevel);
    }



    // Methods for endpoints
    private static async Task<IResult> getUsersByOrg( AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from db
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or the org, or if they are not level 3 or higher
        if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(3)) return Results.Forbid();

        List<User> users = await db.UserAccessLevels.Where(u => u.OrgID == currentUser.OrgID).ToListAsync();
        return users.Any() ? Results.Ok(users) : Results.NotFound();
        
    }


    private static async Task<IResult> regUserToOrg(string joinCode, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app
        if (!currentUser.isRegistered()) return Results.Forbid();
        // Reject if the user is already bound to an org
        if (currentUser.OrgID != 0) return Results.BadRequest("User already assigned to an organisation.");
    
        OrgJoinCode? dbJoinCode = await db.OrgJoinCodes.FirstOrDefaultAsync(j => j.Code == joinCode);

        // Error cases 
        if (dbJoinCode == null) return Results.NotFound();
        if (dbJoinCode.IsUsed) return Results.BadRequest("Join code already used.");
        if (dbJoinCode.ExpiryDate < DateTime.UtcNow) return Results.BadRequest("Join code has expired.");

        // Fetch user from db
        User user = await db.UserAccessLevels.FirstAsync(u => u.UserID == currentUser.UserID);

        // Update fields
        user.OrgID = dbJoinCode.OrgID;
        dbJoinCode.IsUsed = true;

        // Save back to db
        await db.SaveChangesAsync();

        return Results.Ok();
        
    }


    private static async Task<IResult> releaseUserFromOrg(int userId, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // Reject if the user is not registered to the app or is not an admin
        if (!currentUser.isRegistered() || currentUser.hasAccessLevel(3)) return Results.Forbid();
   
        User? user = await db.UserAccessLevels.FirstOrDefaultAsync(u => u.UserID == userId);

        if (user == null) return Results.BadRequest("User does not exist.");
        // Reject if user does not belong to same org as out current user
        if (user.OrgID != currentUser.OrgID) return Results.Forbid();

        // Set OrgId to impossible value
        user.OrgID = -1;

        // Save back to db
        await db.SaveChangesAsync();

        return Results.Ok();
    }


    private static async Task<IResult> deleteUser(AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();
        // Get current user from DB
        await currentUser.getUserFromDBAsync();

        // Get user by the currently logged in user
        User? user = await db.UserAccessLevels.FindAsync(currentUser.UserID);

        // Should be impossible, but check if current user exists
        if (user == null) return Results.BadRequest("User does not exist.");

        // Remove user and save
        db.UserAccessLevels.Remove(user);
        await db.SaveChangesAsync();

        return Results.Ok();
    }


    private static async Task<IResult> createUser(string name, AppDbContext db, IHttpContextAccessor httpAccessor)
    {

        if (name == null) return Results.BadRequest("No name provided");

        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (! currentUser.validateTokenAsync()) return Results.Unauthorized();

        // Check if user exists with this google account
        User? existsQuery = await db.UserAccessLevels.FirstOrDefaultAsync(u => u.GoogleSub == currentUser.GoogleSub);
        if (existsQuery != null) return Results.Conflict("User already exists");

        try {

        // Create new user obj and fill with google sub and name
        User newUser = new User();
        newUser.GoogleSub   = currentUser.GoogleSub;
        newUser.Name        = name;
        newUser.AccessLevel = 1;

        db.UserAccessLevels.Add(newUser);
        await db.SaveChangesAsync();

        }
        catch
        {
            return Results.BadRequest($"Bad stuff here: {name}    {currentUser.GoogleSub}");
        }

        return Results.Created();
    }

    private static async Task<IResult> updateUserAccessLevel(int userId, int newAL, AppDbContext db, IHttpContextAccessor httpAccessor)
    {
        CurrentUser currentUser = new CurrentUser(db, httpAccessor);
        // Reject if user isnt authed by google
        if (!currentUser.validateTokenAsync()) return Results.Unauthorized();

        // Get user to update
        User? user = await db.UserAccessLevels.FindAsync(userId);
        if (user == null) return Results.Conflict("User does not exists");

        // Reject if current user is in different org or is lower level of access or attempting to change their own access level, or is not an admin
        if (user.OrgID != currentUser.OrgID || user.AccessLevel > currentUser.AccessLevel || currentUser.UserID == user.UserID || currentUser.AccessLevel >= 3) return Results.Forbid();


        // If the new access level is legitimate and does not exceed the current users level, set and save
        if (newAL >= 1 && newAL <= currentUser.AccessLevel )
        {
            user.AccessLevel = newAL;
            await db.SaveChangesAsync();
            return Results.Ok();
        }
                
        return Results.Forbid();
        
    }



}
