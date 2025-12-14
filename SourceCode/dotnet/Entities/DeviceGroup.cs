using System.ComponentModel.DataAnnotations;

public class DeviceGroup
{
    [Key]
    public int DeviceGroupID {get; set;}
    public string GroupName {get; set;} = string.Empty;
    public string GPSProtectionMethod {get; set;} = string.Empty;
    public int GPSAccuracy {get; set;}
    public int OrgID {get; set;}
}
