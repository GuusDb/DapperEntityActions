using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DapperOrmCore.Tests.Models;

[Table("plant")]
public class Plant
{
    [Key]
    [Column("plant_cd")]
    public required string PlantCd { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsAcive { get; set; }
}