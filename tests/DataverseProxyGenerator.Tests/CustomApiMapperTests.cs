using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Templates;

namespace DataverseProxyGenerator.Tests;

public class CustomApiMapperTests
{
    [Theory]

    // Value types: nullable suffix depends only on IsOptional (nullableTypes flag ignored).
    [InlineData(CustomApiParameterType.BooleanType, false, true, "bool")]
    [InlineData(CustomApiParameterType.BooleanType, true, true, "bool?")]
    [InlineData(CustomApiParameterType.BooleanType, true, false, "bool?")]
    [InlineData(CustomApiParameterType.DateTimeType, false, true, "System.DateTime")]
    [InlineData(CustomApiParameterType.DateTimeType, true, true, "System.DateTime?")]
    [InlineData(CustomApiParameterType.DecimalType, false, true, "decimal")]
    [InlineData(CustomApiParameterType.DecimalType, true, true, "decimal?")]
    [InlineData(CustomApiParameterType.FloatType, false, true, "double")]
    [InlineData(CustomApiParameterType.FloatType, true, true, "double?")]
    [InlineData(CustomApiParameterType.IntegerType, false, true, "int")]
    [InlineData(CustomApiParameterType.IntegerType, true, true, "int?")]
    [InlineData(CustomApiParameterType.GuidType, false, true, "System.Guid")]
    [InlineData(CustomApiParameterType.GuidType, true, true, "System.Guid?")]

    // Reference types: nullable suffix requires IsOptional AND nullableTypes.
    [InlineData(CustomApiParameterType.EntityType, false, true, "Microsoft.Xrm.Sdk.Entity")]
    [InlineData(CustomApiParameterType.EntityType, true, true, "Microsoft.Xrm.Sdk.Entity?")]
    [InlineData(CustomApiParameterType.EntityType, true, false, "Microsoft.Xrm.Sdk.Entity")]
    [InlineData(CustomApiParameterType.EntityCollectionType, true, true, "Microsoft.Xrm.Sdk.EntityCollection?")]
    [InlineData(CustomApiParameterType.EntityCollectionType, true, false, "Microsoft.Xrm.Sdk.EntityCollection")]
    [InlineData(CustomApiParameterType.EntityReferenceType, true, true, "Microsoft.Xrm.Sdk.EntityReference?")]
    [InlineData(CustomApiParameterType.EntityReferenceType, true, false, "Microsoft.Xrm.Sdk.EntityReference")]
    [InlineData(CustomApiParameterType.MoneyType, true, true, "Microsoft.Xrm.Sdk.Money?")]
    [InlineData(CustomApiParameterType.MoneyType, true, false, "Microsoft.Xrm.Sdk.Money")]
    [InlineData(CustomApiParameterType.PicklistType, true, true, "Microsoft.Xrm.Sdk.OptionSetValue?")]
    [InlineData(CustomApiParameterType.PicklistType, true, false, "Microsoft.Xrm.Sdk.OptionSetValue")]
    [InlineData(CustomApiParameterType.StringType, false, true, "string")]
    [InlineData(CustomApiParameterType.StringType, true, true, "string?")]
    [InlineData(CustomApiParameterType.StringType, true, false, "string")]
    [InlineData(CustomApiParameterType.StringArrayType, true, true, "string[]?")]
    [InlineData(CustomApiParameterType.StringArrayType, true, false, "string[]")]
    public void GetCSharpType_MapsAllParameterTypes(CustomApiParameterType type, bool isOptional, bool nullableTypes, string expected)
    {
        var actual = CustomApiMapper.GetCSharpType(type, isOptional, nullableTypes);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetCSharpType_UnknownType_ReturnsObject()
    {
        var actual = CustomApiMapper.GetCSharpType((CustomApiParameterType)9999, isOptional: false, nullableTypes: true);
        Assert.Equal("object", actual);
    }

    [Fact]
    public void GetXmlDocComment_ReturnsEmpty_ForEmptyDescription()
    {
        Assert.Equal(string.Empty, CustomApiMapper.GetXmlDocComment(string.Empty));
    }

    [Fact]
    public void GetXmlDocComment_WrapsInSummaryTags()
    {
        var actual = CustomApiMapper.GetXmlDocComment("The account id to update.");
        Assert.Equal("/// <summary>\n/// The account id to update.\n/// </summary>", actual);
    }

    [Fact]
    public void GetXmlDocComment_WithSpecialCharacters_EscapesThem()
    {
        var actual = CustomApiMapper.GetXmlDocComment("Amount must be < 1000 & > 0");
        Assert.Equal("/// <summary>\n/// Amount must be &lt; 1000 &amp; &gt; 0\n/// </summary>", actual);
    }

    [Fact]
    public void GetXmlDocComment_WithMultipleLines_PrefixesEveryLine()
    {
        var actual = CustomApiMapper.GetXmlDocComment("First line\r\nSecond line\nThird line");
        Assert.Equal("/// <summary>\n/// First line\n/// Second line\n/// Third line\n/// </summary>", actual);
    }

    [Fact]
    public void MapToTemplateModel_ThrowsOnNullCustomApi()
    {
        Assert.Throws<ArgumentNullException>(() => CustomApiMapper.MapToTemplateModel(null!, CreateContext()));
    }

    [Fact]
    public void MapToTemplateModel_ThrowsOnNullContext()
    {
        var api = new CustomApiModel { UniqueName = "new_MyApi" };
        Assert.Throws<ArgumentNullException>(() => CustomApiMapper.MapToTemplateModel(api, null!));
    }

    private static GenerationContext CreateContext()
    {
        return new GenerationContext
        {
            Namespace = "TestNamespace",
            Version = "1.0.0.0",
            Templates = new EmbeddedTemplateProvider(),
        };
    }
}