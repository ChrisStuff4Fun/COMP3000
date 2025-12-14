using System.ComponentModel.DataAnnotations;

public class OrgJoinCode
{
    [Key]
    public int JoinCodeID {get; set;}
    public int OrgID {get; set;}
    public string Code {get; set;} = string.Empty;
    public DateTime ExpiryDate {get; set;}
    public bool IsUsed {get; set;}
}
