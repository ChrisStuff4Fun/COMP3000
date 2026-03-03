using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

public class CurrentUser
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IDataProtector _protector;

    public int UserID { get; private set; } = -1;
    public int OrgID { get; private set; } = 0;
    public string GoogleSub { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int AccessLevel { get; private set; } = 0;

    // Manual override for testing, allows all api calls to go through
    private bool debugMode = false;

    public CurrentUser(AppDbContext db, IHttpContextAccessor http, IDataProtector protector)
    {
        _db = db;
        _http = http;
        _protector = protector;
    }

    // Check if user is authed with google, checking if the user is even logged in
    public bool validateToken()
    {
        if (debugMode) return true;

        var context = _http.HttpContext;
        if (context == null) return false;

        if (!context.Request.Cookies.TryGetValue("auth", out var protectedValue))
            return false;

        if (string.IsNullOrWhiteSpace(protectedValue))
            return false;

        try
        {
            // Unprotect the cookie value
            GoogleSub = _protector.Unprotect(protectedValue);
        }
        catch
        {
            // Cookie was tampered with or invalid
            return false;
        }

        return true;
    }

    // Check if user is signed up to the app, if so, load data into vars
    public async Task getUserFromDBAsync()
    {
        if (debugMode) return;

        if (string.IsNullOrEmpty(GoogleSub)) return;

        User? user = await _db.UserAccessLevels.FirstOrDefaultAsync(u => u.GoogleSub == GoogleSub);
        if (user != null)
        {


            UserID      = user.UserID;
            OrgID       = user.OrgID;
            AccessLevel = user.AccessLevel; 
            Name        = user.Name;
          


        }
    }

    // Helpers
    public bool isRegistered() => UserID != -1 || debugMode;
    public bool isRegToOrg(int orgId) => OrgID > 0 && OrgID == orgId || debugMode;
    public bool hasAccessLevel(int level) => AccessLevel >= level || debugMode;


}
