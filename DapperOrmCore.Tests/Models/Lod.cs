using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DapperOrmCore.Tests.Models;

[Table("lod")]
public class Lod
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("lod_cd")]
    public required string LodCd { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}