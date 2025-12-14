using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public int UserID {get; set;}
    public int OrgID {get; set;}
    public string GoogleSub {get; set;} = string.Empty;
    public int AccessLevel {get; set;}
    public string Name { get; set; } = string.Empty;
}
