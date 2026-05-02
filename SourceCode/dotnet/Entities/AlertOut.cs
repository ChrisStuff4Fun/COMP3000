using System.ComponentModel.DataAnnotations;

public class AlertOut
{
    public string? GeofenceName {get; set;}
    public string? DeviceName {get; set;}
    public bool AlertOnEnterTriggered {get; set;}
    public bool AlertOnLeaveTriggered {get; set;}
    public DateTime LastUpdated {get; set;}

}
