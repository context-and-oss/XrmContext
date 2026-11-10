using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class IntersectionInterfaceMapper
{
    public static object MapToTemplateModel((string InterfaceName, IEnumerable<ColumnSignature> Columns, IList<TableModel> Tables) input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input.InterfaceName);

        var (interfaceName, colSigs, tables) = input;
        var columns = colSigs
            .Select(sig =>
                tables
                .SelectMany(t => t.Columns)
                .FirstOrDefault(c => ColumnSignature.From(c) == sig))
            .Where(c => c != null)
            .Cast<ColumnModel>();

        var sanitizedInterfaceName = GenerationUtilities.SanitizeName(interfaceName);

        var columnData = columns.Select(col => new
        {
            SchemaName = GenerationUtilities.SanitizeName(col.SchemaName),
            col.DisplayName,
            col.Description,
            TypeSignature = TypeSignatureHelper.GetPropertyTypeSignature(col, context.NullableTypes),
        });

        return new
        {
            Name = sanitizedInterfaceName,
            Columns = columnData,
        };
    }
}