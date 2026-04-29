using System.ComponentModel.DataAnnotations;

public class Device
{
    [Key]
    public int DeviceID {get; set;}
    public string DeviceName {get; set;} = string.Empty;
    public string LastLoggedLat {get; set;} = string.Empty;
    public string LastLoggedLong {get; set;} = string.Empty;
    public int OrgID {get; set;}
    public string PublicKeyX {get; set;} = string.Empty;
    public string PublicKeyY {get; set;} = string.Empty;

}

