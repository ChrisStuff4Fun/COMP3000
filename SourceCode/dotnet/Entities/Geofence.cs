public class Geofence
{
    public int GeofenceID {get; set;}
    public required string GPSCoordinates {get; set;}
    public required string ShapeType {get; set;}
    public double CircleRadius {get; set;}
    public int OrgID {get; set;}
}
