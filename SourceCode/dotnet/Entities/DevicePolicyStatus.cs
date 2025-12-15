using System.ComponentModel.DataAnnotations;

public class DevicePolicyStatus
{
    [Key]
    public int DevicePolicyStatusID {get; set;}
    public int DeviceID {get; set;}
    public int PolicyID {get; set;}
    public bool IsInsideFence {get; set;}
    public bool AlertOnEnterTriggered {get; set;}
    public bool AlertOnLeaveTriggered {get; set;}
    public DateTime LastUpdated {get; set;}
    public int OrgID {get; set;}
}
