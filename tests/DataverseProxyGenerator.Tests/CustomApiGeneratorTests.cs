using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;
using DataverseProxyGenerator.Core.Generation.Generators;
using DataverseProxyGenerator.Core.Templates;

namespace DataverseProxyGenerator.Tests;

public class CustomApiGeneratorTests
{
    [Fact]
    public async Task Generates_Request_And_Response_For_All_Parameter_Types()
    {
        var customApi = BuildAllTypesCustomApi(uniqueName: "new_TestApi", isFunction: false);

        var files = await Generate(customApi, nullableTypes: true).ToListAsync();

        var request = files.Single(f => f.Filename.EndsWith("new_TestApiRequest.cs", StringComparison.Ordinal));
        var response = files.Single(f => f.Filename.EndsWith("new_TestApiResponse.cs", StringComparison.Ordinal));

        await Verify(new
        {
            request = request.Content,
            response = response.Content,
        });
    }

    [Fact]
    public async Task Generates_Two_Files_Per_CustomApi()
    {
        var customApi = new CustomApiModel
        {
            UniqueName = "new_MinimalApi",
            DisplayName = "Minimal Api",
        };

        var files = await Generate(customApi).ToListAsync();

        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.Filename.EndsWith("new_MinimalApiRequest.cs", StringComparison.Ordinal));
        Assert.Contains(files, f => f.Filename.EndsWith("new_MinimalApiResponse.cs", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Generated_Files_Are_Placed_Under_CustomApis_Folder()
    {
        var customApi = new CustomApiModel { UniqueName = "new_MinimalApi" };

        var files = await Generate(customApi).ToListAsync();

        Assert.All(files, f => Assert.StartsWith("customapis", f.Filename, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Filename_Uses_Sanitized_UniqueName()
    {
        // Publisher-prefixed unique names contain an underscore; sanitizer produces a valid C# identifier.
        var customApi = new CustomApiModel { UniqueName = "publisher_Do-Something!" };

        var files = await Generate(customApi).ToListAsync();

        Assert.All(files, f =>
        {
            Assert.DoesNotContain("!", f.Filename, StringComparison.Ordinal);
            Assert.DoesNotContain("-", f.Filename, StringComparison.Ordinal);
        });
        Assert.Contains(files, f => f.Filename.EndsWith("Request.cs", StringComparison.Ordinal));
        Assert.Contains(files, f => f.Filename.EndsWith("Response.cs", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Request_Uses_UniqueName_As_RequestName_And_ProxyAttribute()
    {
        var customApi = new CustomApiModel
        {
            UniqueName = "new_FetchThings",
            DisplayName = "Fetch Things",
        };

        var files = await Generate(customApi).ToListAsync();
        var request = files.Single(f => f.Filename.EndsWith("Request.cs", StringComparison.Ordinal));
        var response = files.Single(f => f.Filename.EndsWith("Response.cs", StringComparison.Ordinal));

        Assert.Contains("RequestProxyAttribute(\"new_FetchThings\")", request.Content, StringComparison.Ordinal);
        Assert.Contains("this.RequestName = \"new_FetchThings\";", request.Content, StringComparison.Ordinal);
        Assert.Contains("ResponseProxyAttribute(\"new_FetchThings\")", response.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Request_Initializes_Only_NonOptional_Parameters_In_Constructor()
    {
        var customApi = new CustomApiModel
        {
            UniqueName = "new_MixedApi",
            RequestParameters = new List<CustomApiParameterModel>
            {
                new() { UniqueName = "Required", Type = CustomApiParameterType.StringType, IsOptional = false },
                new() { UniqueName = "Optional", Type = CustomApiParameterType.StringType, IsOptional = true },
            },
        };

        var files = await Generate(customApi).ToListAsync();
        var request = files.Single(f => f.Filename.EndsWith("Request.cs", StringComparison.Ordinal));

        Assert.Contains("this.Required = default(string);", request.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("this.Optional = default(", request.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Response_Ctor_Accepts_All_Properties_With_CamelCase_Argument_Names()
    {
        var customApi = new CustomApiModel
        {
            UniqueName = "new_LookupApi",
            ResponseProperties = new List<CustomApiParameterModel>
            {
                new() { UniqueName = "AccountId", Type = CustomApiParameterType.GuidType },
                new() { UniqueName = "Name", Type = CustomApiParameterType.StringType },
            },
        };

        var files = await Generate(customApi).ToListAsync();
        var response = files.Single(f => f.Filename.EndsWith("Response.cs", StringComparison.Ordinal));

        // Argument names are the property unique name with the first character lower-cased.
        Assert.Contains("(System.Guid accountId, string name)", response.Content, StringComparison.Ordinal);
        Assert.Contains("this.AccountId = accountId;", response.Content, StringComparison.Ordinal);
        Assert.Contains("this.Name = name;", response.Content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public async Task Response_Only_Generates_Parameterized_Constructor_When_Properties_Exist(bool singleFile, int propertyCount)
    {
        var customApi = new CustomApiModel
        {
            UniqueName = "new_ConstructorApi",
            ResponseProperties = Enumerable.Range(0, propertyCount)
                .Select(i => new CustomApiParameterModel
                {
                    UniqueName = $"Value{i}",
                    Type = CustomApiParameterType.IntegerType,
                }).ToList(),
        };
        var config = new XrmGenerationConfig(
            OutputDirectory: "output",
            NamespaceSetting: "TestNamespace",
            ServiceContextName: "Xrm",
            IntersectMapping: new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture),
            SingleFile: singleFile,
            GenerateCustomApis: true);

        var files = await new CSharpProxyGenerator().GenerateCodeAsync([], [customApi], config).ToListAsync();
        var response = singleFile ? files.Single() : files.Single(f => f.Filename.EndsWith("Response.cs", StringComparison.Ordinal));
        var request = singleFile ? files.Single() : files.Single(f => f.Filename.EndsWith("Request.cs", StringComparison.Ordinal));
        var responseConstructors = response.Content.Split('\n')
            .Where(line => line.TrimStart().StartsWith("public new_ConstructorApiResponse(", StringComparison.Ordinal)).ToList();
        var requestConstructors = request.Content.Split('\n')
            .Where(line => line.TrimStart().StartsWith("public new_ConstructorApiRequest(", StringComparison.Ordinal));

        Assert.Equal(propertyCount == 0 ? 1 : 2, responseConstructors.Count);
        Assert.Single(responseConstructors, line => line.Trim() == "public new_ConstructorApiResponse()");
        Assert.Single(requestConstructors);
        Assert.Contains("public new_ConstructorApiRequest()", request.Content, StringComparison.Ordinal);
        Assert.Contains("this.RequestName = \"new_ConstructorApi\";", request.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Description_Becomes_XmlDoc_Summary_On_Property()
    {
        var customApi = new CustomApiModel
        {
            UniqueName = "new_DescribedApi",
            RequestParameters = new List<CustomApiParameterModel>
            {
                new()
                {
                    UniqueName = "Target",
                    Type = CustomApiParameterType.EntityReferenceType,
                    Description = "The record to act on.",
                },
            },
        };

        var files = await Generate(customApi).ToListAsync();
        var request = files.Single(f => f.Filename.EndsWith("Request.cs", StringComparison.Ordinal));

        Assert.Contains("/// <summary>", request.Content, StringComparison.Ordinal);
        Assert.Contains("/// The record to act on.", request.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NullableTypes_False_Disables_Nullable_On_Reference_Type_Parameters()
    {
        var customApi = new CustomApiModel
        {
            UniqueName = "new_RefApi",
            RequestParameters = new List<CustomApiParameterModel>
            {
                new() { UniqueName = "OptionalString", Type = CustomApiParameterType.StringType, IsOptional = true },
            },
        };

        var files = await Generate(customApi, nullableTypes: false).ToListAsync();
        var request = files.Single(f => f.Filename.EndsWith("Request.cs", StringComparison.Ordinal));

        Assert.Contains("public string OptionalString", request.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("public string? OptionalString", request.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomApi_Not_Emitted_When_GenerateCustomApis_Is_False()
    {
        var customApi = new CustomApiModel { UniqueName = "new_SkippedApi" };
        var config = new XrmGenerationConfig(
            OutputDirectory: "output",
            NamespaceSetting: "TestNamespace",
            ServiceContextName: "Xrm",
            IntersectMapping: new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture),
            GenerateCustomApis: false);

        var files = new CSharpProxyGenerator()
            .GenerateCodeAsync([], [customApi], config);

        Assert.DoesNotContain(files, f => f.Filename.Contains("new_SkippedApi", StringComparison.Ordinal));
    }

    private static IAsyncEnumerable<GeneratedFile> Generate(CustomApiModel customApi, bool nullableTypes = true)
    {
        var context = new GenerationContext
        {
            Namespace = "TestNamespace",
            Version = "1.0.0.0",
            Templates = new EmbeddedTemplateProvider(),
            ServiceContextName = "Xrm",
            NullableTypes = nullableTypes,
        };

        return new CustomApiGenerator().GenerateAsync(customApi, context);
    }

    private static CustomApiModel BuildAllTypesCustomApi(string uniqueName, bool isFunction)
    {
        var parameters = new List<CustomApiParameterModel>
        {
            new() { UniqueName = "BoolIn", Type = CustomApiParameterType.BooleanType, IsOptional = false, Description = "A required boolean." },
            new() { UniqueName = "DateTimeIn", Type = CustomApiParameterType.DateTimeType, IsOptional = true },
            new() { UniqueName = "DecimalIn", Type = CustomApiParameterType.DecimalType, IsOptional = false },
            new() { UniqueName = "EntityIn", Type = CustomApiParameterType.EntityType, IsOptional = true, LogicalEntityName = "account" },
            new() { UniqueName = "EntityCollectionIn", Type = CustomApiParameterType.EntityCollectionType, IsOptional = true },
            new() { UniqueName = "EntityReferenceIn", Type = CustomApiParameterType.EntityReferenceType, IsOptional = false, LogicalEntityName = "contact" },
            new() { UniqueName = "FloatIn", Type = CustomApiParameterType.FloatType, IsOptional = false },
            new() { UniqueName = "IntegerIn", Type = CustomApiParameterType.IntegerType, IsOptional = true },
            new() { UniqueName = "MoneyIn", Type = CustomApiParameterType.MoneyType, IsOptional = true },
            new() { UniqueName = "PicklistIn", Type = CustomApiParameterType.PicklistType, IsOptional = true },
            new() { UniqueName = "StringIn", Type = CustomApiParameterType.StringType, IsOptional = false, Description = "A required string." },
            new() { UniqueName = "StringArrayIn", Type = CustomApiParameterType.StringArrayType, IsOptional = true },
            new() { UniqueName = "GuidIn", Type = CustomApiParameterType.GuidType, IsOptional = false },
        };

        var responseProps = new List<CustomApiParameterModel>
        {
            new() { UniqueName = "BoolOut", Type = CustomApiParameterType.BooleanType },
            new() { UniqueName = "StringOut", Type = CustomApiParameterType.StringType, Description = "The string result." },
            new() { UniqueName = "EntityOut", Type = CustomApiParameterType.EntityType, LogicalEntityName = "account" },
            new() { UniqueName = "GuidOut", Type = CustomApiParameterType.GuidType },
        };

        return new CustomApiModel
        {
            UniqueName = uniqueName,
            DisplayName = "Test Api",
            Description = "A test custom API.",
            IsFunction = isFunction,
            RequestParameters = parameters,
            ResponseProperties = responseProps,
        };
    }
}