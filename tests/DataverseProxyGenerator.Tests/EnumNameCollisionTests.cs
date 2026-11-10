using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;

namespace DataverseProxyGenerator.Tests;

public sealed class EnumNameCollisionTests
{
    [Fact]
    public async Task Generate_TableAndOptionsetCollide_EmitsSuffixedEnumAndColumnType()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_ticket"),
            Table("prefix_case", TicketTypeColumn()),
        };

        // Act
        var files = await GenerateAsync(tables);

        // Assert
        Assert.DoesNotContain(files, f => f.Filename == Path.Combine("optionsets", "prefix_ticket.cs"));
        var enumFile = Assert.Single(files, f => f.Filename == Path.Combine("optionsets", "prefix_ticket_Enum.cs"));
        var tableFile = Assert.Single(files, f => f.Filename == Path.Combine("tables", "prefix_case.cs"));
        Assert.Contains("public prefix_ticket_Enum? prefix_TicketType", tableFile.Content, StringComparison.Ordinal);
        await Verify(new { enumFile = enumFile.Content, tableFile = tableFile.Content });
    }

    [Fact]
    public async Task Generate_MultiSelectCollision_UsesSuffixedEnumInCollection()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_ticket"),
            Table("prefix_case", TicketTypeColumn(isMultiSelect: true)),
        };

        // Act
        var files = await GenerateAsync(tables);

        // Assert
        var tableFile = Assert.Single(files, f => f.Filename == Path.Combine("tables", "prefix_case.cs"));
        Assert.Contains("IEnumerable<prefix_ticket_Enum> prefix_TicketType", tableFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_SingleFile_UsesSuffixedEnum()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_ticket"),
            Table("prefix_case", TicketTypeColumn()),
        };

        // Act
        var files = await GenerateAsync(tables, singleFile: true);

        // Assert
        var content = Assert.Single(files).Content;
        Assert.Contains("public partial class prefix_ticket ", content, StringComparison.Ordinal);
        Assert.Contains("public enum prefix_ticket_Enum", content, StringComparison.Ordinal);
        Assert.DoesNotContain("public enum prefix_ticket\n", content.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains("public prefix_ticket_Enum? prefix_TicketType", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_IntersectionInterface_IncludesSharedEnumColumn()
    {
        // Arrange
        var priority = new EnumColumnModel
        {
            LogicalName = "prefix_priority",
            SchemaName = "prefix_Priority",
            DisplayName = "Priority",
            OptionsetName = "prefix_priority",
            IsGlobalOptionset = true,
            OptionsetValues = new Dictionary<int, string> { { 1, "Low" }, { 2, "High" } },
        };
        var tables = new[]
        {
            Table("prefix_ticket", priority),
            Table("prefix_case", priority),
        };
        var intersectMapping = new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture)
        {
            ["IPrioritized"] = ["prefix_ticket", "prefix_case"],
        };

        // Act
        var files = await GenerateAsync(tables, intersectMapping: intersectMapping);

        // Assert
        var interfaceFile = Assert.Single(files, f => f.Filename == Path.Combine("intersections", "IPrioritized.cs"));
        Assert.Contains("prefix_priority? prefix_Priority { get; set; }", interfaceFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_IntersectionInterface_UsesSuffixedEnum()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_ticket", TicketTypeColumn()),
            Table("prefix_case", TicketTypeColumn()),
        };
        var intersectMapping = new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture)
        {
            ["ITicketTyped"] = ["prefix_ticket", "prefix_case"],
        };

        // Act
        var files = await GenerateAsync(tables, intersectMapping: intersectMapping);

        // Assert
        var interfaceFile = Assert.Single(files, f => f.Filename == Path.Combine("intersections", "ITicketTyped.cs"));
        Assert.Contains("prefix_ticket_Enum? prefix_TicketType", interfaceFile.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_CaseOnlyCollision_FileNamesDistinctIgnoringCase()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_Ticket"),
            Table(
                "prefix_case",
                TicketTypeColumn(),
                new EnumColumnModel
                {
                    LogicalName = "prefix_ticketenum",
                    SchemaName = "prefix_TicketEnum",
                    DisplayName = "Ticket Enum",
                    OptionsetName = "prefix_ticket_enum",
                    IsGlobalOptionset = true,
                    OptionsetValues = new Dictionary<int, string> { { 1, "One" } },
                }),
        };

        // Act
        var files = await GenerateAsync(tables);

        // Assert
        var filenames = files.Select(f => f.Filename).ToList();
        Assert.Equal(filenames.Count, filenames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(Path.Combine("optionsets", "prefix_ticket_Enum2.cs"), filenames);
    }

    private static async Task<List<GeneratedFile>> GenerateAsync(
        IEnumerable<TableModel> tables,
        bool singleFile = false,
        Dictionary<string, IReadOnlyList<string>>? intersectMapping = null)
    {
        var config = new XrmGenerationConfig(
            OutputDirectory: "Output",
            NamespaceSetting: "TestNamespace",
            ServiceContextName: "TestContextName",
            IntersectMapping: intersectMapping ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture),
            SingleFile: singleFile,
            GenerateCustomApis: false);

        return await new CSharpProxyGenerator()
            .GenerateCodeAsync(tables, [], config)
            .ToListAsync();
    }

    private static TableModel Table(string schemaName, params ColumnModel[] columns)
    {
        return new TableModel
        {
            SchemaName = schemaName,
            LogicalName = schemaName.ToLowerInvariant(),
            DisplayName = schemaName,
            PrimaryIdAttribute = $"{schemaName.ToLowerInvariant()}id",
            PrimaryNameAttribute = "prefix_name",
            Columns = columns,
        };
    }

    private static EnumColumnModel TicketTypeColumn(bool isMultiSelect = false)
    {
        return new EnumColumnModel
        {
            LogicalName = "prefix_tickettype",
            SchemaName = "prefix_TicketType",
            DisplayName = "Ticket Type",
            OptionsetName = "prefix_ticket",
            IsGlobalOptionset = true,
            IsMultiSelect = isMultiSelect,
            OptionsetValues = new Dictionary<int, string> { { 1, "Incident" }, { 2, "Request" } },
        };
    }
}