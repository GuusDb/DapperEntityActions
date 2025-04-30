using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table("parent")]
public class Parent
{
    [Key]
    [Column("parent_id")]
    public int ParentId { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [NotMapped]
    public List<Child> Children { get; set; } = new List<Child>();
}