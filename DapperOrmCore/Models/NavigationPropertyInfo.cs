using System.Reflection;

namespace DapperOrmCore.Models;
/// <summary>  
/// Represents information about a navigation property in a database context.  
/// </summary>  
public class NavigationPropertyInfo
{
    /// <summary>  
    /// Gets or sets the name of the related table.  
    /// </summary>  
    public string RelatedTableName { get; set; }

    /// <summary>  
    /// Gets or sets the type of the related entity.  
    /// </summary>  
    public Type RelatedType { get; set; }

    /// <summary>  
    /// Gets or sets the name of the foreign key column.  
    /// </summary>  
    public string ForeignKeyColumn { get; set; }

    /// <summary>  
    /// Gets or sets the property information for the navigation property.  
    /// </summary>  
    public PropertyInfo Property { get; set; }
}
