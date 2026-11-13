using System.Xml.Linq;
using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;

namespace DataverseProxyGenerator.Tests;

public sealed class DescriptionEscapingTests
{
    [Fact]
    public async Task Generates_Escaped_Descriptions()
    {
        // Arrange
        var tables = new[] { TableWithSpecialCharacters("prefix_case") };

        // Act
        var files = await GenerateAsync(tables);

        // Assert
        var tableFile = Assert.Single(files, f => f.Filename == Path.Combine("tables", "prefix_case.cs"));
        AssertDocCommentsAreWellFormedXml(tableFile.Content);
        Assert.Contains("[DisplayName(\"The \\\"main\\\" C:\\\\path\")]", tableFile.Content, StringComparison.Ordinal);
        await Verify(tableFile.Content);
    }

    [Fact]
    public async Task Generates_Escaped_Intersection_Descriptions()
    {
        // Arrange
        var tables = new[] { TableWithSpecialCharacters("prefix_case"), TableWithSpecialCharacters("prefix_ticket") };
        var intersectMapping = new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture)
        {
            ["IShared"] = ["prefix_case", "prefix_ticket"],
        };

        // Act
        var files = await GenerateAsync(tables, intersectMapping);

        // Assert
        var interfaceFile = Assert.Single(files, f => f.Filename == Path.Combine("intersections", "IShared.cs"));
        AssertDocCommentsAreWellFormedXml(interfaceFile.Content);
        Assert.Contains("Amount must be &lt; 1000 &amp; &gt; 0", interfaceFile.Content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Parses every block of consecutive <c>///</c> lines as XML, like the compiler does for CS1570.
    /// </summary>
    private static void AssertDocCommentsAreWellFormedXml(string content)
    {
        var block = new List<string>();
        foreach (var line in content.ReplaceLineEndings("\n").Split('\n').Append(string.Empty))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("///", StringComparison.Ordinal))
            {
                block.Add(trimmed[3..]);
                continue;
            }

            if (block.Count > 0)
            {
                var xml = string.Join("\n", block);
                var exception = Record.Exception(() => XElement.Parse($"<doc>{xml}</doc>"));
                Assert.True(exception == null, $"Malformed XML doc comment:\n{xml}\n{exception?.Message}");
                block.Clear();
            }
        }
    }

    private static TableModel TableWithSpecialCharacters(string schemaName)
    {
        var amount = new StringColumnModel
        {
            LogicalName = "prefix_amount",
            SchemaName = "prefix_Amount",
            DisplayName = "The \"main\" C:\\path",
            Description = "Amount must be < 1000 & > 0\nSee <docs> & notes",
        };

        return new TableModel
        {
            SchemaName = schemaName,
            LogicalName = schemaName,
            DisplayName = "Cases & <Tickets>",
            Description = "Use when a < b\nand R&D is involved",
            PrimaryIdAttribute = $"{schemaName}id",
            PrimaryNameAttribute = "prefix_name",
            Columns = [amount],
            Keys = [new AlternateKeyModel { SchemaName = "prefix_amountkey", DisplayName = "Amount <key> & more", KeyAttributes = [amount] }],
        };
    }

    private static async Task<List<GeneratedFile>> GenerateAsync(
        IEnumerable<TableModel> tables,
        Dictionary<string, IReadOnlyList<string>>? intersectMapping = null)
    {
        var config = new XrmGenerationConfig(
            OutputDirectory: "Output",
            NamespaceSetting: "TestNamespace",
            ServiceContextName: "TestContextName",
            IntersectMapping: intersectMapping ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture),
            GenerateCustomApis: false);

        return await new CSharpProxyGenerator()
            .GenerateCodeAsync(tables, [], config)
            .ToListAsync();
    }
}