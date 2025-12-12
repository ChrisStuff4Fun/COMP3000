public class DeviceJoinCode
{
    public int DeviceJoinCodeID {get; set;}
    public int OrgID {get; set;}
    public string Code {get; set;} = string.Empty;
    public DateTime ExpiryDate {get; set;}
    public bool IsUsed {get; set;}
}
