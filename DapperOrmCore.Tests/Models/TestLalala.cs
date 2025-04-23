using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DapperOrmCore.Tests.Models;

[Table("test")]
public class TestLalala
{
    [Key]
    [Column("test_cd")]
    public required string TestCd { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("test_type_cd")]
    public string TestType { get; set; } = "Dimensional";

    [Column("test_mode_cd")]
    public string TestMode { get; set; } = "InProcess";

    [Column("precision")]
    public int Precision { get; set; } = 80;

    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("last_edit_date")]
    public DateTime LastEditDate { get; set; } = DateTime.UtcNow;
}