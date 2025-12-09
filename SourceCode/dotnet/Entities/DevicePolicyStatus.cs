public class DevicePolicyStatus
{
    public int DevicePolicyStatusID {get; set;}
    public int DeviceID {get; set;}
    public int PolicyID {get; set;}
    public bool IsInsideFence {get; set;}
    public bool AlertOnEnterTriggered {get; set;}
    public bool AlertOnLeaveTriggered {get; set;}
    public DateTime LastUpdated {get; set;}
}
