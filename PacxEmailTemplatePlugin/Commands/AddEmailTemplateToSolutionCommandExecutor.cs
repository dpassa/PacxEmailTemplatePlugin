using Greg.Xrm.Command;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace PacxEmailTemplatePlugin.Commands
{
    /// <summary>
    /// Executor for the AddEmailTemplateToSolutionCommand.
    /// </summary>
    public class AddEmailTemplateToSolutionCommandExecutor : ICommandExecutor<AddEmailTemplateToSolutionCommand>
    {
        /// <summary>
        /// Component type code for Email Template in Dynamics 365.
        /// </summary>
        private const int EmailTemplateComponentType = 36;

        private readonly IOutput _output;
        private readonly IOrganizationServiceAsync2 _organizationService;

        public AddEmailTemplateToSolutionCommandExecutor(
            IOutput output,
            IOrganizationServiceAsync2 organizationService)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        public async Task<CommandResult> ExecuteAsync(AddEmailTemplateToSolutionCommand command, CancellationToken cancellationToken)
        {
            try
            {
                if (command.TemplateId == Guid.Empty)
                {
                    _output.WriteLine("Error: TemplateId is required and must be a valid GUID.", ConsoleColor.Red);
                    return CommandResult.Fail("TemplateId is required and must be a valid GUID.");
                }

                if (string.IsNullOrWhiteSpace(command.SolutionUniqueName))
                {
                    _output.WriteLine("Error: SolutionUniqueName is required.", ConsoleColor.Red);
                    return CommandResult.Fail("SolutionUniqueName is required.");
                }

                _output.WriteLine($"Adding email template {command.TemplateId} to solution: {command.SolutionUniqueName}", ConsoleColor.Cyan);

                // Verify the template exists
                var templateQuery = new QueryExpression("template")
                {
                    ColumnSet = new ColumnSet("templateid", "title"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("templateid", ConditionOperator.Equal, command.TemplateId)
                        }
                    },
                    TopCount = 1
                };

                var templateResults = await _organizationService.RetrieveMultipleAsync(templateQuery, cancellationToken);
                if (templateResults.Entities.Count == 0)
                {
                    _output.WriteLine($"Error: Email template with ID {command.TemplateId} not found.", ConsoleColor.Red);
                    return CommandResult.Fail($"Email template with ID {command.TemplateId} not found.");
                }

                var templateTitle = templateResults.Entities[0].Contains("title") 
                    ? templateResults.Entities[0]["title"].ToString() 
                    : "Unknown";

                _output.WriteLine($"Found template: {templateTitle}", ConsoleColor.Yellow);

                // Get the solution ID
                var solutionQuery = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("solutionid", "friendlyname"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("uniquename", ConditionOperator.Equal, command.SolutionUniqueName)
                        }
                    },
                    TopCount = 1
                };

                var solutionResults = await _organizationService.RetrieveMultipleAsync(solutionQuery, cancellationToken);
                if (solutionResults.Entities.Count == 0)
                {
                    _output.WriteLine($"Error: Solution with unique name '{command.SolutionUniqueName}' not found.", ConsoleColor.Red);
                    return CommandResult.Fail($"Solution with unique name '{command.SolutionUniqueName}' not found.");
                }

                var solutionId = solutionResults.Entities[0].Id;
                var solutionName = solutionResults.Entities[0].Contains("friendlyname") 
                    ? solutionResults.Entities[0]["friendlyname"].ToString() 
                    : command.SolutionUniqueName;

                _output.WriteLine($"Found solution: {solutionName} (ID: {solutionId})", ConsoleColor.Yellow);

                // Add solution component request
                var request = new AddSolutionComponentRequest
                {
                    ComponentId = command.TemplateId,
                    ComponentType = EmailTemplateComponentType,
                    SolutionUniqueName = command.SolutionUniqueName,
                    AddRequiredComponents = command.AddRequiredComponents,
                    DoNotIncludeSubcomponents = command.DoNotIncludeSubcomponents
                };

                _output.WriteLine("Adding component to solution...", ConsoleColor.Yellow);
                
                var response = await _organizationService.ExecuteAsync(request, cancellationToken) as AddSolutionComponentResponse;

                if (response != null && response.id != Guid.Empty)
                {
                    _output.WriteLine($"Successfully added email template '{templateTitle}' to solution '{solutionName}'.", ConsoleColor.Green);
                    _output.WriteLine($"Component ID: {response.id}", ConsoleColor.Green);
                    
                    var result = CommandResult.Success();
                    result["ComponentId"] = response.id;
                    result["TemplateId"] = command.TemplateId;
                    result["TemplateTitle"] = templateTitle ?? string.Empty;
                    result["SolutionName"] = solutionName ?? string.Empty;
                    result["SolutionUniqueName"] = command.SolutionUniqueName;
                    return result;
                }
                else
                {
                    _output.WriteLine("Warning: AddSolutionComponent request completed but no component ID was returned.", ConsoleColor.Yellow);
                    
                    var result = CommandResult.Success();
                    result["Warning"] = "No component ID returned";
                    return result;
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error adding email template to solution: {ex.Message}", ConsoleColor.Red);
                return CommandResult.Fail($"Error adding email template to solution: {ex.Message}", ex);
            }
        }
    }
}
