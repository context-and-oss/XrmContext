using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Generators;
using DataverseProxyGenerator.Core.Generation.Utilities;
using DataverseProxyGenerator.Core.Templates;

namespace DataverseProxyGenerator.Core.Generation;

public class CSharpProxyGenerator : ICodeGenerator
{
    private readonly EmbeddedTemplateProvider templateProvider;
    private readonly ProxyClassGenerator proxyClassGenerator;
    private readonly EnumGenerator enumGenerator;
    private readonly IntersectionInterfaceGenerator intersectionInterfaceGenerator;
    private readonly XrmContextGenerator xrmContextGenerator;
    private readonly HelperFileGenerator helperFileGenerator;
    private readonly CustomApiGenerator customApiGenerator;
    private readonly SingleFileGenerator singleFileGenerator;

    public CSharpProxyGenerator()
    {
        templateProvider = new EmbeddedTemplateProvider();
        proxyClassGenerator = new ProxyClassGenerator();
        enumGenerator = new EnumGenerator();
        intersectionInterfaceGenerator = new IntersectionInterfaceGenerator();
        xrmContextGenerator = new XrmContextGenerator();
        helperFileGenerator = new HelperFileGenerator();
        customApiGenerator = new CustomApiGenerator();
        singleFileGenerator = new SingleFileGenerator();
    }

    private static string GetAssemblyVersion()
    {
        var assembly = typeof(CSharpProxyGenerator).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0.0";
    }

    public IAsyncEnumerable<GeneratedFile> GenerateCodeAsync(IEnumerable<TableModel> tables, IEnumerable<CustomApiModel> customApis, XrmGenerationConfig config)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(config);

        var context = CreateGenerationContext(config);
        var tablesList = EnumNameResolver.Resolve(tables.ToList()).ToList();
        var (interfaceColumns, tableToInterfaces) = PrepareIntersectionData(tablesList, config);
        var customApiList = config.GenerateCustomApis ? customApis.ToList() : new List<CustomApiModel>();

        if (config.SingleFile)
        {
            var interfaceColumnsReadOnly = interfaceColumns.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlySet<ColumnSignature>)kvp.Value,
                StringComparer.InvariantCulture);
            var tableToInterfacesReadOnly = tableToInterfaces.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value,
                StringComparer.InvariantCulture);
            return singleFileGenerator.GenerateAsync((tablesList, interfaceColumnsReadOnly, tableToInterfacesReadOnly, customApiList), context);
        }

        return GenerateMultipleFilesAsync(tablesList, interfaceColumns, tableToInterfaces, customApiList, context);
    }

    private GenerationContext CreateGenerationContext(XrmGenerationConfig config)
    {
        return new GenerationContext
        {
            Namespace = config.NamespaceSetting ?? "DataverseContext",
            Version = GetAssemblyVersion(),
            Templates = templateProvider,
            ServiceContextName = string.IsNullOrEmpty(config.ServiceContextName) ? "Xrm" : config.ServiceContextName,
            IntersectMapping = config.IntersectMapping,
            NullableTypes = config.NullableTypes,
        };
    }

    private static (Dictionary<string, HashSet<ColumnSignature>> InterfaceColumns, Dictionary<string, List<string>> TableToInterfaces)
        PrepareIntersectionData(List<TableModel> tablesList, XrmGenerationConfig config)
    {
        var tableDict = tablesList.ToDictionary(t => t.LogicalName, t => t, StringComparer.InvariantCulture);
        var tableColumns = BuildTableColumns(tablesList);
        return BuildIntersectionData(config.IntersectMapping, tableDict, tableColumns);
    }

    private IAsyncEnumerable<GeneratedFile> GenerateMultipleFilesAsync(
        List<TableModel> tablesList,
        Dictionary<string, HashSet<ColumnSignature>> interfaceColumns,
        Dictionary<string, List<string>> tableToInterfaces,
        IEnumerable<CustomApiModel> customApiList,
        GenerationContext context)
    {
        var globalOptionsetsMulti = GenerationUtilities.GetGlobalOptionsets(tablesList);

        // Generate intersection interfaces
        return interfaceColumns.ToAsyncEnumerable().SelectMany(kvp => intersectionInterfaceGenerator.GenerateAsync((kvp.Key, kvp.Value, tablesList), context))
            .Concat(tablesList.ToAsyncEnumerable().SelectMany(table => // Generate proxy classes
            {
                var interfaces = tableToInterfaces.TryGetValue(table.LogicalName, out var iFaces) ? iFaces : new List<string>();
                return proxyClassGenerator.GenerateAsync((table, interfaces), context);
            }))
            .Concat(globalOptionsetsMulti.ToAsyncEnumerable().SelectMany(optionset => enumGenerator.GenerateAsync(optionset, context))) // Generate enums
            .Concat(xrmContextGenerator.GenerateAsync(tablesList, context)) // Generate Xrm context class
            .Concat(helperFileGenerator.GenerateAsync("OptionSetMetadataAttribute", context)) // Generate helper files
            .Concat(helperFileGenerator.GenerateAsync("RelationshipMetadataAttribute", context))
            .Concat(helperFileGenerator.GenerateAsync("TableAttributeHelpers", context))
            .Concat(helperFileGenerator.GenerateAsync("ExtendedEntity", context))
            .Concat(customApiList.ToAsyncEnumerable().SelectMany(customApi => customApiGenerator.GenerateAsync(customApi, context))); // Generate custom API request/response classes
    }

    // --- Extracted Helper Methods ---
    private static Dictionary<string, HashSet<ColumnSignature>> BuildTableColumns(IEnumerable<TableModel> tables)
    {
        var tableColumns = new Dictionary<string, HashSet<ColumnSignature>>(StringComparer.InvariantCulture);
        foreach (var t in tables)
        {
            tableColumns[t.LogicalName] = t.Columns.Select(ColumnSignature.From).ToHashSet();
        }

        return tableColumns;
    }

    private static (Dictionary<string, HashSet<ColumnSignature>> InterfaceColumns, Dictionary<string, List<string>> TableToInterfaces)
        BuildIntersectionData(IReadOnlyDictionary<string, IReadOnlyList<string>> intersectMapping, Dictionary<string, TableModel> tableDict, Dictionary<string, HashSet<ColumnSignature>> tableColumns)
    {
        var interfaceColumns = new Dictionary<string, HashSet<ColumnSignature>>(StringComparer.InvariantCulture);
        var tableToInterfaces = new Dictionary<string, List<string>>(StringComparer.InvariantCulture);

        if (intersectMapping != null && intersectMapping.Count > 0)
        {
            foreach (var kvp in intersectMapping)
            {
                var interfaceName = kvp.Key;
                var tableNames = kvp.Value.Where(tableDict.ContainsKey).ToList();
                if (tableNames.Count == 0)
                    continue;

                var sets = tableNames.Select(n => tableColumns[n]).ToList();
                var intersection = new HashSet<ColumnSignature>(sets[0]);
                foreach (var s in sets.Skip(1))
                    intersection.IntersectWith(s);

                if (intersection.Count > 0)
                {
                    interfaceColumns[interfaceName] = intersection;
                }

                foreach (var tableName in tableNames)
                {
                    if (!tableToInterfaces.TryGetValue(tableName, out var list))
                    {
                        list = new List<string>();
                        tableToInterfaces[tableName] = list;
                    }

                    if (!list.Contains(interfaceName, StringComparer.InvariantCulture))
                        list.Add(interfaceName);
                }
            }
        }

        return (interfaceColumns, tableToInterfaces);
    }
}