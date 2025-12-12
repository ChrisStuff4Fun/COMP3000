public class Geofence
{
    public int GeofenceID {get; set;}
    public string GPSCoordinates {get; set;} = string.Empty;
    public string ShapeType {get; set;} = string.Empty;
    public double CircleRadius {get; set;}
    public int OrgID {get; set;}
}
