using System.ComponentModel.DataAnnotations;

public class DeviceStatusOut
{
    public string? PolicyName {get; set;}
    public string? DeviceName {get; set;}
    public bool IsInsideFence {get; set;}
    public DateTime LastUpdated {get; set;}

}
