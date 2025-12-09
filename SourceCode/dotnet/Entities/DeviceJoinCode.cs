public class DeviceJoinCode
{
    public int DeviceJoinCodeID {get; set;}
    public int OrgID {get; set;}
    public required string Code {get; set;}
    public DateTime ExpiryDate {get; set;}
    public bool IsUsed {get; set;}
    public DateTime CreatedOn {get; set;}
}
