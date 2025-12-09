public class Policy
{
    public int PolicyID {get; set;}
    public required string PolicyName {get; set;}
    public int GeofenceID {get; set;}
    public int DeviceGroupID {get; set;}
    public bool AlertOnLeaveRule {get; set;}
    public bool AlertOnEnterRule {get; set;}
    public bool TrackInsideFenceRule {get; set;}
    public bool TrackOutsideFenceRule {get; set;}
    public int OrgID {get; set;}
}
