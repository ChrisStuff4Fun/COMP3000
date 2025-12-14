using System.ComponentModel.DataAnnotations;

public class Organisation
{
    [Key]
    public int OrgID {get; set;}
    public string OrgName {get; set;} = string.Empty;

}
