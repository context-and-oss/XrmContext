using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Generation.Common;

/// <summary>
/// Helper struct for fast column comparison.
/// </summary>
public readonly record struct ColumnSignature(string SchemaName, string TypeName)
{
    /// <summary>
    /// Creates the signature of a column. Enum columns include the option set name, so columns with the same
    /// schema name but different option sets are not considered equal.
    /// </summary>
    /// <param name="column">The column model.</param>
    /// <returns>The column signature.</returns>
    public static ColumnSignature From(ColumnModel column)
    {
        ArgumentNullException.ThrowIfNull(column);

        return column is EnumColumnModel enumColumn
            ? new ColumnSignature(column.SchemaName, $"{column.TypeName}:{enumColumn.OptionsetName}")
            : new ColumnSignature(column.SchemaName, column.TypeName);
    }
}