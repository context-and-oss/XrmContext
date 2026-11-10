# Enum name collision and description escaping — design

Status: approved 2026-11-10

## Problem 1 — table / option set name collision

A table and an option set may share a name (table `prefix_ticket`, global option set `prefix_ticket`).
Both are generated into the same namespace, which fails with CS0101.

### Decisions

- Rename the enum only when it collides, by appending `_Enum` (same as XrmContext v3).
  If the suffixed name is also taken, use `_Enum2`, `_Enum3`, ...
- Collisions are checked against table class names only (v3 parity), and against other enum names
  so a suffixed name can never clash with an existing enum.
- Comparison is case-insensitive (`prefix_Ticket` table collides with `prefix_ticket` option set).
  This also keeps file names distinct on case-insensitive file systems.
- Local and global option sets are treated the same. Multi-select columns follow automatically.
- Not configurable.

### Design

A new `EnumNameResolver` (`Generation/Utilities`) runs once in `CSharpProxyGenerator.GenerateCodeAsync`,
directly after materialising the tables and before intersection data and the single/multi-file branch.
It returns new `TableModel`s where every `EnumColumnModel.OptionsetName` is replaced with the final,
sanitized C# type name. All existing consumers (`EnumMapper`, `EnumGenerator`, `TypeSignatureHelper`,
`ProxyClassMapper`, `BuildTableColumns`, `SingleFileMapper`) then agree on the name without changes.
This relies on `NameSanitizer.SanitizeName` being idempotent.

Algorithm:

1. Taken names = sanitized table `SchemaName`s (`OrdinalIgnoreCase`).
2. Distinct option set names (sanitized), ordered ordinally for determinism.
3. Keep pass: every enum name that is not taken keeps its name and becomes taken.
4. Rename pass: every remaining enum gets the first free `{name}_Enum`, `{name}_Enum2`, ...

### Known limitations

- Adding a table later can rename an existing enum (also true in v3).
- Collisions with the service context, intersection interfaces, custom API types and helper types
  are out of scope.

## Problem 2 — Dataverse text inserted into C# without context escaping

Match v3 (CodeDom) behaviour per C# context, plus XML escaping of doc comments:

- `NameSanitizer.EscapeXmlDoc` escapes `&`, `<`, `>` (`&` first). Fixes CS1570.
- Scriban functions `xml_doc` and `cs_string` (wrapping `SanitizeString`) are registered in
  `BaseFileGenerator.CreateTemplateContext` and used in `EntityClass` (table/column description and
  display name, alternate key display name, `[DisplayName("...")]`) and `IntersectionInterface`.
- `CustomApiMapper.GetXmlDocComment` splits multi-line descriptions and prefixes every line with `/// `.
- Difference from v3: newlines in string literals are removed (existing `SanitizeString` behaviour)
  rather than emitted as `\n`.

## Testing

TDD in `tests/DataverseProxyGenerator.Tests`, Verify snapshots plus targeted asserts:

- `EnumNameResolverTests`: unit tests of the naming rule.
- `EnumNameCollisionTests`: end-to-end through `CSharpProxyGenerator` — exact collision, case-only
  collision, multi-select, `_Enum2` fallback, local option set, no collision unchanged, single-file
  mode, intersection interface, order independence.
- `NameSanitizerTests`: `EscapeXmlDoc`.
- Escaping snapshot test: descriptions with `<`, `&`, `>`, newlines; display name with `"`;
  multi-line custom API description.

## Docs

README: the `_Enum` rule. MIGRATION.MD: v3 note (same rule, now case-insensitive).

## Addendum — found during implementation

- Alternate key attributes (`AlternateKeyModel.KeyAttributes`) are separate column instances used as
  parameter types in `Retrieve_{Key}`. The resolver rewrites them too.
- Existing bug fixed (approved): enum columns were never included in intersection interfaces because
  `IntersectionInterfaceMapper` compared the enum signature `EnumColumnModel:{OptionsetName}` with the
  plain type name. Both sides now use `ColumnSignature.From`.
