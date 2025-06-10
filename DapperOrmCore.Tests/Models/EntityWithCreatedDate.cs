using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DapperOrmCore.Tests.Models;

[Table("entity_with_created_date")]
public class EntityWithCreatedDate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("created_date")]
    public DateTime CreatedDate { get; set; }
}