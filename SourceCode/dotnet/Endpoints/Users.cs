using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/users");

        // Map endpoints
        users.MapGet("/byorg/{orgId:int}", getUsersByOrg);
        users.MapGet("/byentra/{entraId:int}", getUserByEntra);
    }



    // Methods for endpoints
    private static async Task<IResult> getUsersByOrg(int orgId, AppDbContext db)
    {
        var users = await db.UserAccessLevels.Where(u => u.OrgID == orgId).ToListAsync();
        return users != null ? Results.Ok(users) : Results.NotFound();
    }

    private static async Task<IResult> getUserByEntra(int entraId, AppDbContext db)
    {
        var user = db.UserAccessLevels.FirstOrDefault(u => u.EntraID == entraId);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }





}
