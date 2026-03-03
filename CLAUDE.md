# PacxEmailTemplatePlugin — Claude Code Guide

## Project Overview

A .NET 8.0 plugin for [PACX (Greg.Xrm.Command)](https://github.com/neronotte/Greg.Xrm.Command), a CLI tool for Microsoft Dataverse/Dynamics 365. This plugin adds two commands for managing email templates:

- `pacx emailtemplate upsert` — Create or update an email template by title
- `pacx emailtemplate addtosolution` — Add an email template as a solution component

## Repository Structure

```
PacxEmailTemplatePlugin/
├── PacxEmailTemplatePlugin.slnx        # Solution file
├── README.md
├── GETTING_STARTED.md
└── PacxEmailTemplatePlugin/            # Main project
    ├── PacxEmailTemplatePlugin.csproj
    └── Commands/
        ├── OptionAttribute.cs                          # Shared CLI option attribute
        ├── UpsertEmailTemplateCommand.cs               # Command params (upsert)
        ├── UpsertEmailTemplateCommandExecutor.cs       # Business logic (upsert)
        ├── AddEmailTemplateToSolutionCommand.cs        # Command params (add to solution)
        └── AddEmailTemplateToSolutionCommandExecutor.cs# Business logic (add to solution)
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
