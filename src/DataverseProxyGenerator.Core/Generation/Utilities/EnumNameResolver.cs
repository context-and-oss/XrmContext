using System.Globalization;
using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Generation.Utilities;

/// <summary>
/// Resolves option set enum names so they do not collide with table class names in the generated namespace.
/// </summary>
public static class EnumNameResolver
{
    private const string CollisionSuffix = "_Enum";

    /// <summary>
    /// Resolves the C# type name of every option set enum.
    /// An enum keeps its sanitized option set name unless it collides (case-insensitively) with a table class
    /// or another enum, in which case <c>_Enum</c> is appended (<c>_Enum2</c>, <c>_Enum3</c>, ... if that is also taken).
    /// </summary>
    /// <param name="tables">The tables to generate.</param>
    /// <returns>Copies of the tables where <see cref="EnumColumnModel.OptionsetName"/> holds the resolved enum name.</returns>
    public static IReadOnlyList<TableModel> Resolve(IReadOnlyList<TableModel> tables)
    {
        ArgumentNullException.ThrowIfNull(tables);

        var resolvedNames = ResolveNames(tables);

        return tables
            .Select(table => table with
            {
                Columns = ResolveColumns(table.Columns, resolvedNames),
                Keys = table.Keys
                    .Select(key => key with { KeyAttributes = ResolveColumns(key.KeyAttributes, resolvedNames) })
                    .ToList(),
            })
            .ToList();
    }

    private static List<ColumnModel> ResolveColumns(IEnumerable<ColumnModel> columns, Dictionary<string, string> resolvedNames)
    {
        return columns
            .Select(column => column is EnumColumnModel enumColumn && resolvedNames.TryGetValue(enumColumn.OptionsetName, out var resolvedName)
                ? enumColumn with { OptionsetName = resolvedName }
                : column)
            .ToList();
    }

    private static Dictionary<string, string> ResolveNames(IReadOnlyList<TableModel> tables)
    {
        var takenNames = new HashSet<string>(
            tables.Select(t => NameSanitizer.SanitizeName(t.SchemaName)),
            StringComparer.OrdinalIgnoreCase);

        // Order by name so the result does not depend on the order of tables and columns
        var optionsets = tables
            .SelectMany(t => t.Columns)
            .OfType<EnumColumnModel>()
            .Select(c => c.OptionsetName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct(StringComparer.Ordinal)
            .Select(name => (OptionsetName: name, SanitizedName: NameSanitizer.SanitizeName(name)))
            .OrderBy(o => o.SanitizedName, StringComparer.Ordinal)
            .ThenBy(o => o.OptionsetName, StringComparer.Ordinal)
            .ToList();

        var resolvedNames = new Dictionary<string, string>(StringComparer.Ordinal);

        // Reserve all non-colliding names first, so a suffixed name never renames an enum without a collision
        foreach (var (optionsetName, sanitizedName) in optionsets)
        {
            if (takenNames.Add(sanitizedName))
                resolvedNames[optionsetName] = sanitizedName;
        }

        foreach (var (optionsetName, sanitizedName) in optionsets.Where(o => !resolvedNames.ContainsKey(o.OptionsetName)))
        {
            var candidateName = sanitizedName + CollisionSuffix;
            var number = 1;
            while (!takenNames.Add(candidateName))
            {
                number++;
                candidateName = string.Create(CultureInfo.InvariantCulture, $"{sanitizedName}{CollisionSuffix}{number}");
            }

            resolvedNames[optionsetName] = candidateName;
        }

        return resolvedNames;
    }
}