using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class CurrentUser
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public int UserID { get; private set; } = -1;
    public int OrgID { get; private set; } = 0;
    public string GoogleSub { get; private set; } = string.Empty;
     public string Email { get; private set; } = string.Empty;
    public int AccessLevel { get; private set; } = 0;

    // Manual override for testing, allows all api calls to go through
    private bool debugMode = true;

    public CurrentUser(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    // Check if user is authed with google, checking if the user is even logged in
    public async Task<bool> validateGoogleTokenAsync()
    {
        if (debugMode) return true;

        string? token = _http.HttpContext?.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token)) return false;

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(token);
            GoogleSub = payload.Subject;
            Email     = payload.Email;
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Check if user is signed up to the app, if so, load data into vars
    public async Task getUserFromDBAsync()
    {
        if (debugMode) return;

        if (string.IsNullOrEmpty(GoogleSub)) return;

        User? user = await _db.UserAccessLevels.FirstOrDefaultAsync(u => u.GoogleSub == GoogleSub);
        if (user != null)
        {
            UserID = user.UserID;
            OrgID = user.OrgID;
            AccessLevel = user.AccessLevel;

            // Update email if needed 

            if (Email != user.Email)
            {
                user.Email = Email;
                await _db.SaveChangesAsync();
            } 


        }
    }

    // Helpers
    public bool isRegistered() => UserID != -1 || debugMode;
    public bool isRegToOrg(int orgId) => OrgID > 0 && OrgID == orgId || debugMode;
    public bool hasAccessLevel(int level) => AccessLevel >= level || debugMode;
}
