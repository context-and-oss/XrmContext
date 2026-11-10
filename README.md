# XrmContext [![NuGet version](https://badge.fury.io/nu/XrmContext.svg)](https://badge.fury.io/nu/XrmContext)

XrmContext generates early-bound .NET classes for Microsoft Dynamics 365/Dataverse entities. It's similar to CrmSvcUtil or pac modelbuilder but with enhanced features including smaller code files, filtering options, strongly-typed option sets, and helper methods.

## Installation

Install XrmContext as a local dotnet tool for your project:

```bash
# Initialize tool manifest (if not already done)
dotnet new tool-manifest

# Install XrmContext locally
dotnet tool install xrmcontext

# Other team members can restore the tool with:
dotnet tool restore
```

## Configuration

Create an `appsettings.json` file in the root of your repository with your XrmContext configuration:

```json
{
  "XrmContext": {
    "OutputDirectory": "./src/Dataverse",
    "NamespaceSetting": "MyCompany.Dataverse",
    "ServiceContextName": "DataverseContext",
    "Solutions": ["MySolution"],
    "Entities": ["account", "contact", "opportunity"],
    "DeprecatedPrefix": "ZZ_",
    "SingleFile": false,
    "GenerateCustomApis": true,
    "IntersectMapping": {
      "ICustomer": ["account", "contact"]
    },
    "LabelMapping": {
      "\u2714\uFE0F": "checkmark"
    }
  }
}
```

You'll also need to configure your Dataverse connection in the same file. See the [DataverseConnection documentation](https://www.nuget.org/packages/DataverseConnection/) for connection configuration options.

## Usage

Run the tool from your repository root:

```bash
dotnet xrmcontext
```

Override configuration with command-line arguments:

```bash
# Generate specific entities
dotnet xrmcontext --entities account,contact,opportunity

# Output to different directory
dotnet xrmcontext --output ./generated

# Filter by solution
dotnet xrmcontext --solutions MySolution,AnotherSolution

# Use a different namespace
dotnet xrmcontext --namespace MyCompany.CRM
```

## Configuration Options

| Option | Type | Description | Required |
|--------|------|-------------|----------|
| `OutputDirectory` | `string` | Directory where generated files are written | Yes |
| `NamespaceSetting` | `string` | Namespace for generated classes | No (default: "DataverseContext") |
| `ServiceContextName` | `string` | Name of the service context class | No (default: "Xrm") |
| `Solutions` | `string[]` | Array of solution names. Entities and custom APIs are fetched from the solution | No |
| `Entities` | `string[]` | Array of entity logical names to generate. Additional entities beyond those in Solutions | No |
| `DeprecatedPrefix` | `string` | Prefix for marking deprecated attributes | No |
| `SingleFile` | `bool` | Output all classes to a single file | No (default: false) |
| `GenerateCustomApis` | `bool` | Generate custom API classes | No (default: true) |
| `IntersectMapping` | `object` | Map interface names to arrays of entity logical names | No |
| `LabelMapping` | `object` | Map unicode characters to readable strings in labels | No |

## Features

- **Smaller code files** - More efficient code generation compared to CrmSvcUtil
- **Flexible filtering** - Filter by solution or specific entities
- **Strongly-typed option sets** - Option sets generated as enums
- **Simplified attributes** - Direct access without `OptionSetValue` wrappers
- **Helper methods** - Additional utility methods for entities and context
- **Deprecation support** - Mark deprecated attributes with custom prefix
- **Interface mapping** - Group entities under common interfaces
- **DebuggerDisplay** - Better debugging experience with display attributes

## Option Set Name Collisions

Tables and option sets are generated into the same namespace. When an option set has the same name as a table (compared case-insensitively), `_Enum` is appended to the enum name. If that name is also taken, a number is added (`_Enum2`, `_Enum3`, ...). Option sets without a collision keep their name.

For example, a table `prefix_ticket` and a global option set `prefix_ticket` generate:

``` CSharp
public partial class prefix_ticket : ExtendedEntity { ... }
public enum prefix_ticket_Enum { ... }
```

Adding a table later can therefore rename an existing enum.
