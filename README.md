# PacxEmailTemplatePlugin

A PACX plugin to handle common actions for email templates on Dynamics 365.

## Overview

This plugin extends [PACX (Greg.Xrm.Command)](https://github.com/neronotte/Greg.Xrm.Command), a command-line utility belt for Microsoft Dataverse/Dynamics 365, providing two specialized commands for managing email templates:

1. **Upsert Email Template**: Create or update email templates based on a unique key (title)
2. **Add Email Template to Solution**: Add existing email templates as solution components

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

### Deploy as PACX Tool

Copy the built DLL to the PACX plugins directory (typically `~/.pacx/plugins/` on Linux/Mac or `%USERPROFILE%\.pacx\plugins\` on Windows), or install via NuGet once published.

## Commands

### 1. Upsert Email Template

Creates a new email template or updates an existing one based on the title.

#### Usage

```bash
pacx emailtemplate upsert --title "Welcome Email" --subject "Welcome to our service" --body "<html>...</html>"
```

#### Parameters

| Parameter | Alias | Required | Description |
|-----------|-------|----------|-------------|
| `--title` | `-t` | Yes | The title of the email template (used as unique key) |
| `--subject` | `-s` | No | The subject of the email template |
| `--body` | `-b` | No | The body of the email template (HTML) |
| `--description` | `-d` | No | The description of the email template |
| `--templatetypecode` | `-tt` | No | The template type code (default: email) |
| `--mimetype` | `-m` | No | The MIME type |
| `--subjectpresentationxml` | `-spx` | No | The subject presentation XML |
| `--presentationxml` | `-px` | No | The body presentation XML |
| `--languagecode` | `-l` | No | The language code (e.g., 1033 for English) |
| `--ispersonal` | `-p` | No | Whether the template is personal (true/false) |

#### Examples

Create a simple email template:
```bash
pacx emailtemplate upsert \
  --title "Customer Welcome" \
  --subject "Welcome!" \
  --body "<html><body><h1>Welcome to our service!</h1></body></html>" \
  --description "Welcome email for new customers"
```

Update an existing template:
```bash
pacx emailtemplate upsert \
  --title "Customer Welcome" \
  --subject "Welcome to our improved service!" \
  --body "<html><body><h1>Welcome!</h1><p>We've made improvements.</p></body></html>"
```

### 2. Add Email Template to Solution

Adds an existing email template to a Dynamics 365 solution as a solution component.

#### Usage

```bash
pacx emailtemplate addtosolution --templateid <GUID> --solution "MySolution"
```

#### Parameters

| Parameter | Alias | Required | Description |
|-----------|-------|----------|-------------|
| `--templateid` | `-tid` | Yes | The GUID of the email template |
| `--solution` | `-s` | Yes | The unique name of the target solution |
| `--addrequiredcomponents` | `-arc` | No | Include required components (default: false) |
| `--donotincludesubcomponents` | `-dns` | No | Exclude subcomponents (default: false) |

#### Examples

Add a template to a solution:
```bash
pacx emailtemplate addtosolution \
  --templateid "12345678-1234-1234-1234-123456789012" \
  --solution "MyCustomSolution"
```

Add a template with required components:
```bash
pacx emailtemplate addtosolution \
  --templateid "12345678-1234-1234-1234-123456789012" \
  --solution "MyCustomSolution" \
  --addrequiredcomponents true
```

## Technical Details

### Architecture

The plugin follows PACX's command pattern:

- **Command Classes**: Define the structure and parameters for each command
- **Command Executor Classes**: Implement the business logic using the Dataverse SDK

### Dependencies

- **Greg.Xrm.Command.Interfaces**: Core PACX interfaces and abstractions
- **Microsoft.PowerPlatform.Dataverse.Client**: Dataverse/Dynamics 365 client SDK
- **Microsoft.CrmSdk.CoreAssemblies**: Core SDK assemblies for Dataverse operations

### Email Template Entity

The commands operate on the `template` entity in Dynamics 365 with the following key attributes:

- `templateid`: Unique identifier
- `title`: Display name and unique key
- `subject`: Email subject line
- `body`: Email body (HTML)
- `templatetypecode`: Type of template (e.g., "email")
- `description`: Template description
- `languagecode`: Language identifier
- `ispersonal`: Personal vs. organization template flag

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or contributions, please open an issue on the GitHub repository.

## Related Projects

- [PACX (Greg.Xrm.Command)](https://github.com/neronotte/Greg.Xrm.Command) - The core command-line utility
- [XrmToolBox](https://www.xrmtoolbox.com/) - A Windows application for Dynamics 365 customization

## Author

- David Passa (@dpassa)

## Acknowledgments

- Thanks to Riccardo Gregori (@_neronotte) for creating PACX
- Microsoft Dynamics 365 community

