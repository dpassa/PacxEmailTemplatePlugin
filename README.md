# PacxEmailTemplatePlugin

A PACX plugin for managing email templates in Dynamics 365 / Microsoft Dataverse from a local folder structure.

## Overview

This plugin extends [PACX (Greg.Xrm.Command)](https://github.com/neronotte/Greg.Xrm.Command), a command-line utility belt for Microsoft Dataverse/Dynamics 365, providing commands to scaffold, create, push, and manage email templates.

## Prerequisites

- .NET 8.0 or higher
- PACX (Greg.Xrm.Command) installed as a global tool
- Access to a Dynamics 365/Dataverse environment

## Installation

### Build from Source

1. Clone the repository:
```bash
git clone https://github.com/dpassa/PacxEmailTemplatePlugin.git
cd PacxEmailTemplatePlugin
```

2. Build the project:
```bash
dotnet build
```

3. The compiled DLL will be available in `PacxEmailTemplatePlugin/bin/Debug/net8.0/PacxEmailTemplatePlugin.dll`

### Deploy as PACX Plugin

Copy the built DLL to the PACX plugins directory (typically `~/.pacx/plugins/` on Linux/Mac or `%USERPROFILE%\.pacx\plugins\` on Windows), or install via NuGet once published.

## Local Folder Convention

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

`presentationxml` and `subjectpresentationxml` are always derived automatically — never set manually.

## Commands

### 1. `pacx emailtemplate init`

Scaffolds the local folder structure for email templates.

- **Without `--remote`** — creates a single stub folder with an empty HTML file and a `definitions.json`. `--name` is required.
- **With `--remote`** — performs a **bulk pull** from Dynamics 365: every template found in D365 is scaffolded as a local folder. Folders that already exist are skipped automatically. `--name` is optional in this mode and acts as a filter (pull only the template whose title matches).

> **Important:** Always run `--remote` against your **DEV** environment.
> Pulling from DEV ensures version control captures the baseline state of all unmanaged
> (custom) templates and enables the whole team to adopt this tooling on projects that
> already have templates in D365.

```bash
# Local stub (no D365 connection needed)
pacx emailtemplate init --name "Welcome Email"
pacx emailtemplate init --name "Welcome Email" --path ./templates

# Bulk pull — scaffolds ALL templates from D365 into the current directory
pacx emailtemplate init --remote
pacx emailtemplate init --remote --path ./templates

# Single pull — scaffolds only the template whose title matches
pacx emailtemplate init --name "Welcome Email" --remote
pacx emailtemplate init --name "Welcome Email" --path ./templates --remote
```

| Parameter | Alias | Required | Description |
|---|---|---|---|
| `--name` | `-n` | Only without `--remote` | Template name — folder name; optional D365 title filter when `--remote` is used |
| `--path` | `-p` | No | Base directory for the new template folder(s) (default: current directory) |
| `--remote` | `-r` | No | Bulk pull from D365 — scaffolds all unmanaged templates (optionally filtered by `--name`) |

### 2. `pacx emailtemplate create`

Creates a new D365 email template from a local template folder. Reads `definitions.json` and the optional HTML body file. **Fails if a template with the same title already exists** — use `push` to update an existing template.

```bash
pacx emailtemplate create
pacx emailtemplate create --path ./templates/WelcomeEmail
pacx emailtemplate create --solution MyCustomSolution
```

| Parameter | Alias | Required | Description |
|---|---|---|---|
| `--path` | `-p` | No | Path to the template folder (default: current directory) |
| `--solution` | `-s` | No | Solution unique name — adds template to the solution after creation |

### 3. `pacx emailtemplate push`

Updates the subject and body of an existing D365 email template from a local template folder. **Fails if no template with the given title exists** — use `create` to create a new template.

```bash
pacx emailtemplate push
pacx emailtemplate push --path ./templates/WelcomeEmail
pacx emailtemplate push --solution MyCustomSolution
```

| Parameter | Alias | Required | Description |
|---|---|---|---|
| `--path` | `-p` | No | Path to the template folder (default: current directory) |
| `--solution` | `-s` | No | Solution unique name — adds template to the solution after push |

### 4. `pacx emailtemplate addtosolution`

Adds an existing email template to a Dynamics 365 solution as a solution component.

```bash
pacx emailtemplate addtosolution --templateid 12345678-1234-1234-1234-123456789012 --solution MyCustomSolution
pacx emailtemplate addtosolution --templateid 12345678-1234-1234-1234-123456789012 --solution MyCustomSolution --addrequiredcomponents
```

| Parameter | Alias | Required | Description |
|---|---|---|---|
| `--templateid` | `-tid` | Yes | The GUID of the email template |
| `--solution` | `-s` | Yes | The unique name of the target solution |
| `--addrequiredcomponents` | `-arc` | No | Include required components (default: false) |
| `--donotincludesubcomponents` | `-dns` | No | Exclude subcomponents (default: false) |

## Development

### Building

```bash
dotnet build
dotnet pack   # pack manually when needed (GeneratePackageOnBuild is false)
```

Testing is done manually via the PACX CLI — no test project exists.

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Related Projects

- [PACX (Greg.Xrm.Command)](https://github.com/neronotte/Greg.Xrm.Command) - The core command-line utility
- [XrmToolBox](https://www.xrmtoolbox.com/) - A Windows application for Dynamics 365 customization

## Author

David Passa ([@dpassa](https://github.com/dpassa))

## Acknowledgments

- Thanks to Riccardo Gregori ([@_neronotte](https://github.com/neronotte)) for creating PACX
