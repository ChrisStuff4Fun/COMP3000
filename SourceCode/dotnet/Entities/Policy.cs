using System.ComponentModel.DataAnnotations;

public class Policy
{
    [Key]
    public int PolicyID {get; set;}
    public string PolicyName {get; set;} = string.Empty;
    public int GeofenceID {get; set;}
    public int DeviceGroupID {get; set;}
    public bool AlertOnLeaveRule {get; set;}
    public bool AlertOnEnterRule {get; set;}
    public bool TrackInsideFenceRule {get; set;}
    public bool TrackOutsideFenceRule {get; set;}
    public int OrgID {get; set;}
}
