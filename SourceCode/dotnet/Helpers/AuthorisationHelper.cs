public interface ICurrentUser
{
    int UserID {get; set;}
    int? OrgID {get; set;}
    int EntraID {get; set;}
    int AccessLevel {get; set;}
}

public class CurrentUser : ICurrentUser
{
    public int UserID { get; set; }
    public int? OrgID { get; set; }
    public int EntraID { get; set; }
    public int AccessLevel { get; set; }

    // Helper function to check if action is permitted for given user
    public bool isUserAuthorised(int requiredAuthLevel)
    {
        return AccessLevel >= requiredAuthLevel;
    }

}
