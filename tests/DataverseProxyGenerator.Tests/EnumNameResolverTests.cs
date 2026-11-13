using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Tests;

public sealed class EnumNameResolverTests
{
    [Fact]
    public void Resolve_NoCollision_KeepsOptionsetName()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_case", Enum("prefix_priority", "prefix_priority")),
        };

        // Act
        var result = EnumNameResolver.Resolve(tables);

        // Assert
        Assert.Equal("prefix_priority", OptionsetNameOf(result, "prefix_case", "prefix_priority"));
    }

    [Fact]
    public void Resolve_OptionsetMatchesTable_AppendsEnumSuffix()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_ticket"),
            Table("prefix_case", Enum("prefix_tickettype", "prefix_ticket")),
        };

        // Act
        var result = EnumNameResolver.Resolve(tables);

        // Assert
        Assert.Equal("prefix_ticket_Enum", OptionsetNameOf(result, "prefix_case", "prefix_tickettype"));
    }

    [Fact]
    public void Resolve_OptionsetMatchesTableIgnoringCase_AppendsEnumSuffix()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_Ticket"),
            Table("prefix_case", Enum("prefix_tickettype", "prefix_ticket")),
        };

        // Act
        var result = EnumNameResolver.Resolve(tables);

        // Assert
        Assert.Equal("prefix_ticket_Enum", OptionsetNameOf(result, "prefix_case", "prefix_tickettype"));
    }

    [Fact]
    public void Resolve_SameOptionsetOnSeveralColumns_ResolvesAllColumnsToSameName()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_ticket", Enum("prefix_parenttype", "prefix_ticket")),
            Table("prefix_case", Enum("prefix_tickettype", "prefix_ticket")),
        };

        // Act
        var result = EnumNameResolver.Resolve(tables);

        // Assert
        Assert.Equal("prefix_ticket_Enum", OptionsetNameOf(result, "prefix_ticket", "prefix_parenttype"));
        Assert.Equal("prefix_ticket_Enum", OptionsetNameOf(result, "prefix_case", "prefix_tickettype"));
    }

    [Fact]
    public void Resolve_LocalOptionsetCollision_AppendsEnumSuffix()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_case_prefix_status"),
            Table("prefix_case", Enum("prefix_status", "prefix_case_prefix_status", isGlobal: false)),
        };

        // Act
        var result = EnumNameResolver.Resolve(tables);

        // Assert
        Assert.Equal("prefix_case_prefix_status_Enum", OptionsetNameOf(result, "prefix_case", "prefix_status"));
    }

    [Fact]
    public void Resolve_MultiSelectCollision_AppendsEnumSuffix()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_ticket"),
            Table("prefix_case", Enum("prefix_tickettypes", "prefix_ticket", isMultiSelect: true)),
        };

        // Act
        var result = EnumNameResolver.Resolve(tables);

        // Assert
        Assert.Equal("prefix_ticket_Enum", OptionsetNameOf(result, "prefix_case", "prefix_tickettypes"));
    }

    [Fact]
    public void Resolve_AlternateKeyWithCollidingEnum_ResolvesKeyAttribute()
    {
        // Arrange
        var column = Enum("prefix_tickettype", "prefix_ticket");
        var table = Table("prefix_case", column) with
        {
            Keys = [new AlternateKeyModel { SchemaName = "prefix_tickettypekey", DisplayName = "Ticket Type Key", KeyAttributes = [column] }],
        };
        var tables = new[] { Table("prefix_ticket"), table };

        // Act
        var result = EnumNameResolver.Resolve(tables);

        // Assert
        var keyAttribute = Assert.Single(result.Single(t => t.SchemaName == "prefix_case").Keys.Single().KeyAttributes);
        Assert.Equal("prefix_ticket_Enum", Assert.IsType<EnumColumnModel>(keyAttribute).OptionsetName);
    }

    [Fact]
    public void Resolve_SuffixedNameAlsoTaken_UsesNumberedSuffix()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_ticket"),
            Table(
                "prefix_case",
                Enum("prefix_tickettype", "prefix_ticket"),
                Enum("prefix_ticketenum", "prefix_ticket_enum")),
        };

        // Act
        var result = EnumNameResolver.Resolve(tables);

        // Assert
        Assert.Equal("prefix_ticket_Enum2", OptionsetNameOf(result, "prefix_case", "prefix_tickettype"));
        Assert.Equal("prefix_ticket_enum", OptionsetNameOf(result, "prefix_case", "prefix_ticketenum"));
    }

    [Fact]
    public void Resolve_SuffixedNameMatchesTable_UsesNumberedSuffix()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_ticket"),
            Table("prefix_ticket_Enum"),
            Table("prefix_case", Enum("prefix_tickettype", "prefix_ticket")),
        };

        // Act
        var result = EnumNameResolver.Resolve(tables);

        // Assert
        Assert.Equal("prefix_ticket_Enum2", OptionsetNameOf(result, "prefix_case", "prefix_tickettype"));
    }

    [Fact]
    public void Resolve_TableOrderReversed_ProducesSameNames()
    {
        // Arrange
        var tables = new[]
        {
            Table("prefix_ticket", Enum("prefix_casetype", "prefix_case")),
            Table("prefix_case", Enum("prefix_tickettype", "prefix_ticket"), Enum("prefix_ticketenum", "prefix_ticket_enum")),
            Table("prefix_other", Enum("prefix_priority", "prefix_priority")),
        };

        // Act
        var result = EnumNameResolver.Resolve(tables);
        var reversedResult = EnumNameResolver.Resolve(tables.Reverse().ToList());

        // Assert
        Assert.Equal(EnumNames(result), EnumNames(reversedResult));
    }

    [Fact]
    public void Resolve_DoesNotMutateInput()
    {
        // Arrange
        var column = Enum("prefix_tickettype", "prefix_ticket");
        var table = Table("prefix_case", column);
        var tables = new[] { Table("prefix_ticket"), table };

        // Act
        _ = EnumNameResolver.Resolve(tables);

        // Assert
        Assert.Equal("prefix_ticket", column.OptionsetName);
        Assert.Same(column, Assert.Single(table.Columns));
    }

    private static List<string> EnumNames(IEnumerable<TableModel> tables)
    {
        return tables
            .SelectMany(t => t.Columns.OfType<EnumColumnModel>().Select(c => $"{t.SchemaName}.{c.LogicalName}={c.OptionsetName}"))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static TableModel Table(string schemaName, params ColumnModel[] columns)
    {
        return new TableModel
        {
            SchemaName = schemaName,
            LogicalName = schemaName.ToLowerInvariant(),
            DisplayName = schemaName,
            Columns = columns,
        };
    }

    private static EnumColumnModel Enum(string logicalName, string optionsetName, bool isMultiSelect = false, bool isGlobal = true)
    {
        return new EnumColumnModel
        {
            LogicalName = logicalName,
            SchemaName = logicalName,
            DisplayName = logicalName,
            OptionsetName = optionsetName,
            IsGlobalOptionset = isGlobal,
            IsMultiSelect = isMultiSelect,
            OptionsetValues = new Dictionary<int, string> { { 1, "One" } },
        };
    }

    private static string OptionsetNameOf(IEnumerable<TableModel> tables, string tableSchemaName, string columnLogicalName)
    {
        return tables
            .Single(t => t.SchemaName == tableSchemaName)
            .Columns.OfType<EnumColumnModel>()
            .Single(c => c.LogicalName == columnLogicalName)
            .OptionsetName;
    }
}