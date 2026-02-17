using System.ComponentModel.DataAnnotations;

public class Geofence
{
    [Key]
    public int GeofenceID {get; set;}
    public string GeofenceName {get; set;} = string.Empty;
    public string GeoJSON {get; set;} = string.Empty;
    public int OrgID {get; set;}
}
