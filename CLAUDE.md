# PacxEmailTemplatePlugin — Claude Code Guide

## Project Overview

A .NET 8.0 plugin for [PACX (Greg.Xrm.Command)](https://github.com/neronotte/Greg.Xrm.Command), a CLI tool for Microsoft Dataverse/Dynamics 365. This plugin adds commands for managing email templates from a local folder structure:

- `pacx emailtemplate init` — Scaffold a local template folder (optionally pulling from D365 with `--remote`)
- `pacx emailtemplate create` — Create a new D365 template from a local folder; fails if already exists
- `pacx emailtemplate push` — Update subject/body of an existing D365 template from a local folder; fails if not found
- `pacx emailtemplate addtosolution` — Add an existing template to a D365 solution by ID

## Repository Structure

```
PacxEmailTemplatePlugin/
├── PacxEmailTemplatePlugin.slnx        # Solution file
├── README.md
├── GETTING_STARTED.md
└── PacxEmailTemplatePlugin/            # Main project
    ├── PacxEmailTemplatePlugin.csproj
    └── Commands/
        ├── OptionAttribute.cs                              # Shared CLI option attribute
        ├── InitEmailTemplateCommand.cs                     # Command params (init)
        ├── InitEmailTemplateCommandExecutor.cs             # Business logic (init / --remote pull)
        ├── CreateEmailTemplateCommand.cs                   # Command params (create)
        ├── CreateEmailTemplateCommandExecutor.cs           # Business logic (create-only + additionalFields)
        ├── PushEmailTemplateCommand.cs                     # Command params (push)
        ├── PushEmailTemplateCommandExecutor.cs             # Business logic (update subject+body only)
        ├── AddEmailTemplateToSolutionCommand.cs            # Command params (addtosolution)
        └── AddEmailTemplateToSolutionCommandExecutor.cs    # Business logic (addtosolution)
```

## Build

```bash
dotnet build
dotnet pack   # GeneratePackageOnBuild is false — pack manually when needed
```

No test project exists. Testing is done manually via the PACX CLI.

## Key Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Greg.Xrm.Command.Interfaces` | 1.1.1 | PACX framework (ICommandExecutor, CommandResult, IOutput) |
| `Microsoft.PowerPlatform.Dataverse.Client` | 1.2.10 | Async Dataverse service |
| `Microsoft.CrmSdk.CoreAssemblies` | 9.0.2.60 | Entity, QueryExpression, etc. |

## Architecture Patterns

### Command / Executor Separation
Each command consists of two files:
- **Command class** — Plain C# class with properties decorated with `[Required]` and `[Option]` attributes. Defines what parameters the CLI accepts.
- **Executor class** — Implements `ICommandExecutor<TCommand>`. Contains all business logic and Dataverse calls.

### Local Template Folder Convention
`init`, `create`, and `push` all operate on a two-level folder structure:
```
templates/
├── definitions.json          # ROOT — shared defaults for all templates in this directory
└── MyTemplateName/
    ├── MyTemplateName.html   # optional — email body (HTML); filename must match folder name
    └── definitions.json      # TEMPLATE — specific metadata for this template
```

**Root `definitions.json`** (created once by `init`, shared across all sibling templates):
| Field | Default | Notes |
|---|---|---|
| `templateTypeCode` | `"email"` | Applied to all templates unless overridden |
| `languageCode` | `1033` | LCID, e.g. 1033 = English |
| `isPersonal` | `false` | `true` = scoped to owning user |

**Template `definitions.json`** (one per template folder):
| Field | Required | Used by | Notes |
|---|---|---|---|
| `title` | Yes | create, push | D365 record title — unique key |
| `subject` | Yes | create, push | Email subject line |
| `description` | No | create | — |
| `templateTypeCode` | No | create | Overrides root value |
| `languageCode` | No | create | Overrides root value |
| `isPersonal` | No | create | Overrides root value |
| `additionalFields` | No | create only | `{ "new_field": value }` — types inferred from JSON |

Resolution order: template-level → root-level → hard-coded default.

`presentationxml` and `subjectpresentationxml` are always derived automatically via `WrapInPresentationXml` — never set manually.

### Adding a New Command
1. Create `MyNewCommand.cs` in `Commands/` — properties with `[Option]` and optionally `[Required]`
2. Create `MyNewCommandExecutor.cs` in `Commands/` — implement `ICommandExecutor<MyNewCommand>`

PACX discovers executors automatically via reflection — no manual DI registration needed.

### Coding Conventions
- **Namespaces**: `PacxEmailTemplatePlugin.Commands`
- **Nullable**: Enabled — use `string?` for optional properties, null-coalesce where needed
- **Implicit usings**: Enabled
- All Dataverse I/O is **async** (`*Async` methods) with `CancellationToken` threading throughout
- Use `QueryExpression` with `TopCount = 1` for single-record lookups
- Use `GetAttributeValue<T>()` (never direct dictionary access) for safer null handling
- Constant for component type: `const int EmailTemplateComponentType = 36;`

### Output & Results
```csharp
// Colored console output via IOutput
_output.WriteLine("message", ConsoleColor.Green);   // success
_output.WriteLine("message", ConsoleColor.Yellow);  // info
_output.WriteLine("message", ConsoleColor.Cyan);    // operation
_output.WriteLine("message", ConsoleColor.Red);     // error

// Return structured results
var result = CommandResult.Success();
result["Key"] = value;
return result;

// On failure
return CommandResult.Fail("Error description", ex);
```

### Constructor Pattern
```csharp
public MyExecutor(IOutput output, IOrganizationServiceAsync2 organizationService)
{
    _output = output ?? throw new ArgumentNullException(nameof(output));
    _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
}
```

## Current Branch

`dev` — targeting merge into `main`.

## Notes

- No CI/CD config yet; GETTING_STARTED.md has an Azure DevOps pipeline example
- NuGet package metadata is configured in the `.csproj` (author: David Passa, version: 1.0.0)
- XML doc comments are required on all public classes and properties
