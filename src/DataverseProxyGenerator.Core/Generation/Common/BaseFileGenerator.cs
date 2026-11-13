using DataverseProxyGenerator.Core.Generation.Utilities;
using Scriban;
using Scriban.Runtime;

namespace DataverseProxyGenerator.Core.Generation.Common;

/// <summary>
/// Base class for file generators providing common functionality.
/// </summary>
public abstract class BaseFileGenerator
{
    /// <summary>
    /// Validates the generation context.
    /// </summary>
    /// <param name="context">The generation context to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    protected static void ValidateContext(GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Templates);

        if (string.IsNullOrWhiteSpace(context.Namespace))
        {
            throw new ArgumentException("Namespace cannot be null or empty", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.Version))
        {
            throw new ArgumentException("Version cannot be null or empty", nameof(context));
        }
    }

    /// <summary>
    /// Creates a template context with standard settings and optional TemplateLoader.
    /// </summary>
    /// <param name="model">The model to use for rendering.</param>
    /// <param name="loader">Optional template loader for includes.</param>
    /// <returns>A configured template context.</returns>
    protected static TemplateContext CreateTemplateContext(object model, ITemplateLoader? loader = null)
    {
        var templateContext = new TemplateContext(StringComparer.InvariantCulture)
        {
            LoopLimit = 0, // No limit
            MemberRenamer = member => member.Name, // Preserve original member names
        };

        // Use a ScriptObject that preserves original property names
        var scriptObject = new ScriptObject(StringComparer.InvariantCulture);
        scriptObject.Import(model, renamer: member => member.Name);

        // Escaping of Dataverse text depending on where it is inserted in the generated code
        scriptObject.Import("xml_doc", new Func<string, string>(NameSanitizer.EscapeXmlDoc));
        scriptObject.Import("cs_string", new Func<string, string>(NameSanitizer.SanitizeString));
        templateContext.PushGlobal(scriptObject);

        if (loader != null)
            templateContext.TemplateLoader = loader;
        return templateContext;
    }
}