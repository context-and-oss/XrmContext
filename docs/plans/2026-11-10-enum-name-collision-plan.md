# Enum name collision and description escaping — implementation plan

Design: `2026-11-10-enum-name-collision-design.md`. Branch: `fix/enum-name-collision`.

Every task is red → green → refactor. "Run tests" means
`dotnet test --configuration Release` (Release treats warnings as errors, so analyzer
violations fail the step too). New Verify snapshots are reviewed by hand before being accepted
as `.verified.txt`. One commit per task.

## Task 0 — Baseline

- `dotnet build --configuration Release` and run tests on the unchanged branch. Record result.

## Task 1 — `EnumNameResolver`: names without collisions are unchanged

Test file `EnumNameResolverTests.cs` (plain asserts, AAA comments like `NameSanitizerTests`).
Small local helpers `Table(schemaName, params ColumnModel[])` and `Enum(logicalName, optionsetName, multi = false)`.

- Red: `Resolve_NoCollision_KeepsOptionsetName` — table `prefix_case`, option set `prefix_priority`
  → column keeps `OptionsetName == "prefix_priority"`.
- Green: add `Generation/Utilities/EnumNameResolver.cs`:
  `public static IReadOnlyList<TableModel> Resolve(IReadOnlyList<TableModel> tables)` that returns
  tables with each `EnumColumnModel` rewritten via `with { OptionsetName = map[...] }` (identity map for now).

## Task 2 — exact and case-insensitive collisions get `_Enum`

- Red:
  - `Resolve_OptionsetMatchesTable_AppendsEnumSuffix` — table `prefix_ticket`, enum column on table
    `prefix_case` with option set `prefix_ticket` → `prefix_ticket_Enum`.
  - `Resolve_OptionsetMatchesTableIgnoringCase_AppendsEnumSuffix` — table `prefix_Ticket`.
  - `Resolve_SameOptionsetOnSeveralColumns_ResolvesAllColumnsToSameName` — two tables use it.
  - `Resolve_LocalOptionsetCollision_AppendsEnumSuffix` — `IsGlobalOptionset = false`.
  - `Resolve_MultiSelectCollision_AppendsEnumSuffix`.
- Green: implement the algorithm from the design: taken = sanitized table schema names
  (`OrdinalIgnoreCase`); distinct sanitized option set names ordered `Ordinal`; keep pass; rename pass.

## Task 3 — suffix fallback and determinism

- Red:
  - `Resolve_SuffixedNameAlsoTaken_UsesNumberedSuffix` — table `prefix_ticket`, option sets
    `prefix_ticket` and `prefix_ticket_enum` → `prefix_ticket_Enum2`, `prefix_ticket_enum` unchanged.
  - `Resolve_SuffixedNameMatchesTable_UsesNumberedSuffix` — tables `prefix_ticket` and `prefix_ticket_Enum`.
  - `Resolve_TableOrderReversed_ProducesSameNames`.
  - `Resolve_DoesNotMutateInput` — original `TableModel`s are unchanged.
- Green: numbered loop starting at 2; fix any ordering issue.

## Task 4 — wire into `CSharpProxyGenerator`

Test file `EnumNameCollisionTests.cs` (end-to-end via `CSharpProxyGenerator.GenerateCodeAsync`,
config built like `AttributeTypeCodeGenTests`).

- Red:
  - `Generate_TableAndOptionsetCollide_EmitsSuffixedEnumAndColumnType` — Verify snapshot of the enum
    file and of the referencing table file (`Verify(new { enumFile, tableFile })`), plus asserts:
    file `optionsets/prefix_ticket_Enum.cs` exists, `optionsets/prefix_ticket.cs` does not,
    table file contains `prefix_ticket_Enum?`.
  - `Generate_MultiSelectCollision_UsesSuffixedEnumInCollection` — contains `IEnumerable<prefix_ticket_Enum>`.
  - `Generate_SingleFile_UsesSuffixedEnum` — `SingleFile: true`; contains `public enum prefix_ticket_Enum`
    and `public partial class prefix_ticket`.
  - `Generate_IntersectionInterface_UsesSuffixedEnum` — `IntersectMapping` with two tables sharing the
    colliding enum column; interface file contains `prefix_ticket_Enum?`.
  - `Generate_CaseOnlyCollision_FileNamesDistinctIgnoringCase` — all generated `Filename`s are distinct
    with `OrdinalIgnoreCase` for a model with a case-only collision.
- Green: in `GenerateCodeAsync` replace `var tablesList = tables.ToList();` with
  `var tablesList = EnumNameResolver.Resolve(tables.ToList()).ToList();`.
- Check existing snapshots are unchanged.

## Task 5 — `NameSanitizer.EscapeXmlDoc`

- Red in `NameSanitizerTests.cs`: `EscapeXmlDoc_WithAmpersandAndAngleBrackets_EscapesThem`
  (`a < b & c > d` → `a &lt; b &amp; c &gt; d`), `EscapeXmlDoc_WithAlreadyEscapedEntity_EscapesAmpersand`
  (`&amp;` → `&amp;amp;`), `EscapeXmlDoc_WithNull_ReturnsEmpty`.
- Green: implement with `Replace(..., StringComparison.Ordinal)`, `&` first, same style as `SanitizeString`.

## Task 6 — escaping in table and intersection templates

Test file `DescriptionEscapingTests.cs`.

- Red: `Generates_Escaped_Descriptions` — Verify snapshot of a table whose description, display name,
  column description/display name, and alternate key display name contain `<`, `&`, `>`, a newline,
  and a display name with `"` and `\`. Asserts: no raw `< 1000` inside `///` lines, and the
  `[DisplayName("...")]` line contains `\"`.
  Plus `Generates_Escaped_Intersection_Descriptions` for `IntersectionInterface`.
- Green:
  - `BaseFileGenerator.CreateTemplateContext`: `scriptObject.Import("xml_doc", new Func<string, string>(NameSanitizer.EscapeXmlDoc))`
    and `cs_string` for `SanitizeString`.
  - `EntityClass.scriban-cs`: `{{ line | string.strip | xml_doc }}`, `{{ table.DisplayName | xml_doc }}`,
    `{{ column.DisplayName | xml_doc }}`, `{{ key.DisplayName | xml_doc }}`,
    `[DisplayName("{{ column.DisplayName | cs_string }}")]`.
  - `IntersectionInterface.scriban-cs`: same for description and display name.
- Check `AttributeTypeCodeGenTests` snapshot is unchanged.

## Task 7 — custom API doc comments

- Red in `CustomApiMapperTests.cs`: `GetXmlDocComment_WithSpecialCharacters_EscapesThem`,
  `GetXmlDocComment_WithMultipleLines_PrefixesEveryLine` (`"a\r\nb"` →
  `"/// <summary>\n/// a\n/// b\n/// </summary>"`). Extend the custom API generator snapshot test
  with a parameter whose description is multi-line and contains `<`/`&` (new test, existing snapshot untouched).
- Green: split on `\r\n` / `\n`, escape each line with `EscapeXmlDoc`, join with `\n/// `.

## Task 8 — docs and final check

- README: "Option set name collisions" section with the `_Enum` rule.
- MIGRATION.MD: note for v3 users (same rule, now case-insensitive).
- `dotnet build --configuration Release`, `dotnet test --configuration Release`,
  `dotnet format --verify-no-changes`.
