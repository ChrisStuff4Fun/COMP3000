public class OrgJoinCode
{
    public int JoinCodeID {get; set;}
    public int OrgID {get; set;}
    public required string Code {get; set;}
    public DateTime ExpiryDate {get; set;}
    public bool IsUsed {get; set;}
}
