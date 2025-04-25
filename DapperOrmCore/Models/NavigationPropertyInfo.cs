using System.Reflection;

namespace DapperOrmCore.Models;
public class NavigationPropertyInfo
{
    public string RelatedTableName { get; set; }
    public Type RelatedType { get; set; }
    public string ForeignKeyColumn { get; set; }
    public PropertyInfo Property { get; set; }
}