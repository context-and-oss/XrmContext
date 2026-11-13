using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Utilities;
using System.Globalization;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class CustomApiMapper
{
    public static object MapToTemplateModel(CustomApiModel customApi, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(customApi);
        ArgumentNullException.ThrowIfNull(context);

        var argCounter = new Counter();
        return new
        {
            unique_name = customApi.UniqueName,
            sanitized_unique_name = GenerationUtilities.SanitizeName(customApi.UniqueName),
            display_name = customApi.DisplayName,
            description = customApi.Description,
            is_function = customApi.IsFunction,
            request_parameters = customApi.RequestParameters.Select(p => new
            {
                name = GenerationUtilities.SanitizeName(p.UniqueName),
                original_name = p.Name,
                unique_name = p.UniqueName,
                display_name = p.DisplayName,
                description = p.Description,
                csharp_type = GetCSharpType(p.Type, p.IsOptional, context.NullableTypes),
                is_optional = p.IsOptional,
                logical_entity_name = p.LogicalEntityName,
                xml_doc_comment = GetXmlDocComment(p.Description),
            }).ToList(),
            response_properties = customApi.ResponseProperties.Select(p => new
            {
                name = GenerationUtilities.SanitizeName(p.UniqueName),
                original_name = p.Name,
                unique_name = p.UniqueName,
                display_name = p.DisplayName,
                argument_name = ToLowerFirst(p.UniqueName, argCounter),
                description = p.Description,
                csharp_type = GetCSharpType(p.Type, p.IsOptional, context.NullableTypes),
                is_optional = p.IsOptional,
                logical_entity_name = p.LogicalEntityName,
                xml_doc_comment = GetXmlDocComment(p.Description),
            }).ToList(),
        };
    }

    public static string GetCSharpType(CustomApiParameterType type, bool isOptional, bool nullableTypes)
    {
        return type switch
        {
            CustomApiParameterType.BooleanType => isOptional ? "bool?" : "bool",
            CustomApiParameterType.DateTimeType => isOptional ? "System.DateTime?" : "System.DateTime",
            CustomApiParameterType.DecimalType => isOptional ? "decimal?" : "decimal",
            CustomApiParameterType.EntityType => isOptional && nullableTypes ? "Microsoft.Xrm.Sdk.Entity?" : "Microsoft.Xrm.Sdk.Entity",
            CustomApiParameterType.EntityCollectionType => isOptional && nullableTypes ? "Microsoft.Xrm.Sdk.EntityCollection?" : "Microsoft.Xrm.Sdk.EntityCollection",
            CustomApiParameterType.EntityReferenceType => isOptional && nullableTypes ? "Microsoft.Xrm.Sdk.EntityReference?" : "Microsoft.Xrm.Sdk.EntityReference",
            CustomApiParameterType.FloatType => isOptional ? "double?" : "double",
            CustomApiParameterType.IntegerType => isOptional ? "int?" : "int",
            CustomApiParameterType.MoneyType => isOptional && nullableTypes ? "Microsoft.Xrm.Sdk.Money?" : "Microsoft.Xrm.Sdk.Money",
            CustomApiParameterType.PicklistType => isOptional && nullableTypes ? "Microsoft.Xrm.Sdk.OptionSetValue?" : "Microsoft.Xrm.Sdk.OptionSetValue",
            CustomApiParameterType.StringType => isOptional && nullableTypes ? "string?" : "string",
            CustomApiParameterType.StringArrayType => isOptional && nullableTypes ? "string[]?" : "string[]",
            CustomApiParameterType.GuidType => isOptional ? "System.Guid?" : "System.Guid",
            _ => "object",
        };
    }

    public static string GetXmlDocComment(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        var lines = description
            .ReplaceLineEndings("\n")
            .Split('\n')
            .Select(line => $"/// {NameSanitizer.EscapeXmlDoc(line)}");

        return $"/// <summary>\n{string.Join("\n", lines)}\n/// </summary>";
    }

    /// <summary>
    /// Converts the first character of the given name to lowercase. If the name is null or empty, it generates a fallback name using the provided prefix and a counter.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <param name="counter">The counter used to generate a fallback name if the name is null or empty.</param>
    /// <param name="fallbackPrefix">The prefix to use for the fallback name.</param>
    /// <returns>The converted name with the first character in lowercase, or a fallback name if the original name is null or empty.</returns>
    private static string ToLowerFirst(string name, Counter counter, string fallbackPrefix = "arg")
    {
        if (string.IsNullOrEmpty(name))
        {
            return fallbackPrefix + counter.Increment().ToString(CultureInfo.InvariantCulture);
        }

        return name.Length == 1
            ? name.ToLowerInvariant()
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}