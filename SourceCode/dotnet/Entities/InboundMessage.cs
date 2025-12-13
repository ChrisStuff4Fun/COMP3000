using Microsoft.EntityFrameworkCore.Diagnostics;

public class inboundMessage
{
    public int deviceId;
    public double xGPS;
    public double yGPS; 
    public DateTime timestamp;
    public required byte[] signature;


}