using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table("child")]
public class Child
{
    [Key]
    [Column("child_id")]
    public int ChildId { get; set; }

    [Column("parent_id")]
    [ForeignKey("Parent")]
    public int ParentId { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }
}