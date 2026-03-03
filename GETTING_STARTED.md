# PACX Email Template Plugin - Getting Started

This guide will help you get started with the PACX Email Template Plugin.

## Installation

### Option 1: Install from NuGet (Recommended - when published)

Once published to NuGet, install the plugin as a PACX tool:

```bash
pacx tool install PacxEmailTemplatePlugin
```

### Option 2: Build from Source

1. Clone the repository:
```bash
git clone https://github.com/dpassa/PacxEmailTemplatePlugin.git
cd PacxEmailTemplatePlugin
```

2. Build the project:
```bash
dotnet build --configuration Release
```

3. Copy the DLL to your PACX plugins directory:
   - **Windows**: `%USERPROFILE%\.pacx\plugins\`
   - **Linux/Mac**: `~/.pacx/plugins/`

```bash
# Windows
copy PacxEmailTemplatePlugin\bin\Release\net8.0\PacxEmailTemplatePlugin.dll %USERPROFILE%\.pacx\plugins\

# Linux/Mac
cp PacxEmailTemplatePlugin/bin/Release/net8.0/PacxEmailTemplatePlugin.dll ~/.pacx/plugins/
```

## Prerequisites

Before using the plugin, ensure you have:

1. **PACX installed**: Install the PACX global tool
   ```bash
   dotnet tool install --global Greg.Xrm.Command
   ```

2. **Dataverse connection**: Configure a connection to your Dynamics 365 environment
   ```bash
   pacx auth create
   ```

## Quick Start

### 1. Create an Email Template

Create a simple email template:

```bash
pacx emailtemplate upsert \
  --title "Welcome Email" \
  --subject "Welcome to our service!" \
  --body "<html><body><h1>Welcome!</h1><p>Thank you for joining us.</p></body></html>" \
  --description "Welcome email sent to new customers"
```

### 2. Update an Existing Template

To update, use the same command with the same title:

```bash
pacx emailtemplate upsert \
  --title "Welcome Email" \
  --subject "Welcome to our improved service!" \
  --body "<html><body><h1>Welcome!</h1><p>We've made exciting improvements.</p></body></html>"
```

### 3. Add Template to a Solution

First, get the template ID from the upsert command output, then:

```bash
pacx emailtemplate addtosolution \
  --templateid "12345678-1234-1234-1234-123456789012" \
  --solution "MyCustomSolution"
```

## Advanced Usage

### Creating Multi-Language Templates

Create templates for different languages:

```bash
# English template
pacx emailtemplate upsert \
  --title "Welcome Email" \
  --subject "Welcome!" \
  --body "<html>...</html>" \
  --languagecode 1033

# French template
pacx emailtemplate upsert \
  --title "Email de bienvenue" \
  --subject "Bienvenue!" \
  --body "<html>...</html>" \
  --languagecode 1036
```

### Including Required Components

When adding to a solution, include all required components:

```bash
pacx emailtemplate addtosolution \
  --templateid "12345678-1234-1234-1234-123456789012" \
  --solution "MyCustomSolution" \
  --addrequiredcomponents true
```

### Using Template Type Codes

For non-email templates:

```bash
pacx emailtemplate upsert \
  --title "Custom Template" \
  --templatetypecode "custom" \
  --subject "Custom Subject" \
  --body "<html>...</html>"
```

## Common Language Codes

- **1033**: English (US)
- **1031**: German
- **1036**: French
- **1040**: Italian
- **1034**: Spanish
- **1041**: Japanese
- **2052**: Chinese (Simplified)

## Troubleshooting

### Connection Issues

If you get connection errors:

```bash
# List available connections
pacx auth list

# Select the right connection
pacx auth select

# Test the connection
pacx auth ping
```

### Template Not Found

When adding to solution, ensure:
1. The template ID is correct
2. The template exists in the environment
3. You have permissions to access the template

### Solution Not Found

Ensure:
1. The solution unique name is correct (not the display name)
2. The solution exists in the environment
3. You have permissions to modify the solution

## Best Practices

1. **Use Descriptive Titles**: Template titles should be unique and descriptive
2. **Version Control**: Store template HTML in version control
3. **Testing**: Always test templates in a development environment first
4. **Solution Management**: Add templates to unmanaged solutions for development
5. **Deployment**: Use managed solutions for production deployments

## Integration with CI/CD

You can use these commands in your CI/CD pipelines:

```yaml
# Example Azure DevOps pipeline step
- script: |
    pacx auth create --connectionstring "$(ConnectionString)"
    pacx emailtemplate upsert --title "$(TemplateName)" --body "$(TemplateBody)"
    pacx emailtemplate addtosolution --templateid "$(TemplateId)" --solution "$(SolutionName)"
  displayName: 'Deploy Email Template'
```

## Getting Help

For more information:

- Run `pacx emailtemplate upsert --help`
- Run `pacx emailtemplate addtosolution --help`
- Check the [README.md](README.md) for detailed documentation
- Open an issue on [GitHub](https://github.com/dpassa/PacxEmailTemplatePlugin/issues)

## Examples Repository

Find more examples in the `examples` directory (coming soon) or in the [wiki](https://github.com/dpassa/PacxEmailTemplatePlugin/wiki).
