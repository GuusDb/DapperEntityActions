using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DapperOrmCore.Tests.Models;

[Table("measurement")]
public class CoolMeasurement
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("test_cd")]
    public required string TestCd { get; set; }

    [Column("plant_cd")]
    public required string PlantCd { get; set; }

    [Column("avg_value")]
    public double Value { get; set; }

    [Column("measurement_date")]
    public DateTime MeasurementDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    [ForeignKey("test_cd")]
    public TestLalala Test { get; set; }

    [NotMapped]
    [ForeignKey("plant_cd")]
    public Plant Plant { get; set; }
}