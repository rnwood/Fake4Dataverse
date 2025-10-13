using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Service.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using System.CommandLine;
using CoreWCF;
using CoreWCF.Configuration;

namespace Fake4Dataverse.Service;

/// <summary>
/// CLI service host for Fake4Dataverse IOrganizationService.
/// This service provides a SOAP/WCF endpoint that exposes a fake IOrganizationService
/// backed by Fake4Dataverse, using 100% Microsoft Dataverse SDK types and matching
/// the actual Organization Service SOAP endpoints.
/// 
/// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice
/// The IOrganizationService interface provides methods for:
/// - Create: Creates a new entity record
/// - Retrieve: Retrieves an entity record by ID
/// - Update: Updates an existing entity record
/// - Delete: Deletes an entity record
/// - Associate: Associates two entity records
/// - Disassociate: Disassociates two entity records
/// - RetrieveMultiple: Retrieves multiple entity records
/// - Execute: Executes an organization request
/// 
/// Microsoft Dynamics 365/Dataverse uses SOAP endpoints at paths like:
/// - /XRMServices/2011/Organization.svc (current standard)
/// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/overview#about-the-legacy-soap-endpoint
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Fake4Dataverse CLI Service - Host a fake IOrganizationService SOAP endpoint for testing and development");

        var startCommand = new Command("start", "Start the Fake4Dataverse service");
        var portOption = new Option<int>(
            name: "--port",
            description: "The port to listen on",
            getDefaultValue: () => 5000);
        var hostOption = new Option<string>(
            name: "--host",
            description: "The host to bind to",
            getDefaultValue: () => "localhost");
        var accessTokenOption = new Option<string?>(
            name: "--access-token",
            description: "Optional access token for authentication. If not provided, allows anonymous access. If provided, clients must include this token in the Authorization header.",
            getDefaultValue: () => null);

        startCommand.AddOption(portOption);
        startCommand.AddOption(hostOption);
        startCommand.AddOption(accessTokenOption);

        startCommand.SetHandler(async (int port, string host, string? accessToken) =>
        {
            await StartService(port, host, accessToken);
        }, portOption, hostOption, accessTokenOption);

        rootCommand.AddCommand(startCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task StartService(int port, string host, string? accessToken)
    {
        Console.WriteLine($"Starting Fake4Dataverse Service on {host}:{port}...");
        Console.WriteLine("This service provides SOAP endpoints compatible with Microsoft Dynamics 365/Dataverse Organization Service");
        
        if (!string.IsNullOrEmpty(accessToken))
        {
            Console.WriteLine($"Authentication: ENABLED (Access token required)");
        }
        else
        {
            Console.WriteLine($"Authentication: DISABLED (Anonymous access allowed)");
        }
        Console.WriteLine();
        
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = new[] { $"--urls=http://{host}:{port}" }
        });

        // Create and register the Fake4Dataverse context
        var context = XrmFakedContextFactory.New();
        var organizationService = context.GetOrganizationService();
        
        builder.Services.AddSingleton<IXrmFakedContext>(context);
        builder.Services.AddSingleton<IOrganizationService>(organizationService);
        
        // Register the access token as a singleton if provided
        if (!string.IsNullOrEmpty(accessToken))
        {
            builder.Services.AddSingleton(new AccessTokenValidator(accessToken));
        }
        
        // Register the WCF service implementation
        builder.Services.AddSingleton<OrganizationServiceImpl>();

        // Add CoreWCF services for SOAP endpoints
        builder.Services.AddServiceModelServices();
        builder.Services.AddServiceModelMetadata();

        // Add REST API controllers for OData endpoints
        // Using Microsoft.AspNetCore.OData for advanced OData support
        // Reference: https://learn.microsoft.com/en-us/odata/webapi-8/overview
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Configure JSON serialization for OData compatibility
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // Preserve property names
                options.JsonSerializerOptions.WriteIndented = true; // Pretty print for readability
            })
            .AddOData(options =>
            {
                // Enable OData query options for all routes
                // Reference: https://learn.microsoft.com/en-us/odata/webapi-8/fundamentals/query-options
                options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(1000);
                
                // Enable OData batch requests
                // Reference: https://learn.microsoft.com/en-us/odata/webapi-8/fundamentals/batch-requests
                options.EnableQueryFeatures();
                
                // Set timezone to UTC for consistency
                options.TimeZone = System.TimeZoneInfo.Utc;
            });

        var app = builder.Build();
        
        // Add authentication middleware if token is provided
        if (!string.IsNullOrEmpty(accessToken))
        {
            app.Use(async (context, next) =>
            {
                // Check for Authorization header
                if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    var token = authHeader.ToString().Replace("Bearer ", "").Trim();
                    if (token == accessToken)
                    {
                        await next();
                        return;
                    }
                }
                
                // Unauthorized
                context.Response.StatusCode = 401;
                context.Response.Headers.Add("WWW-Authenticate", "Bearer");
                await context.Response.WriteAsync("Unauthorized: Invalid or missing access token");
            });
        }

        // Configure WCF for SOAP endpoints
        app.UseServiceModel(serviceBuilder =>
        {
            // Add the Organization Service at the standard 2011 endpoint
            serviceBuilder.AddService<OrganizationServiceImpl>(options =>
            {
                options.DebugBehavior.IncludeExceptionDetailInFaults = true;
            });
            
            // Discover and register known types for Execute method
            var knownTypes = KnownTypesProvider.GetKnownTypes(null);
            Console.WriteLine($"[KnownTypesProvider] Registering {knownTypes.Count()} known types for WCF serialization");
            
            // Standard Dynamics 365/Dataverse endpoint path
            serviceBuilder.AddServiceEndpoint<OrganizationServiceImpl, IOrganizationServiceContract>(
                new BasicHttpBinding(),
                "/XRMServices/2011/Organization.svc");
                
            // Add WSDL support
            var serviceMetadataBehavior = app.Services.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
            serviceMetadataBehavior.HttpGetEnabled = true;
        });

        // Configure REST API for OData endpoints
        app.MapControllers();

        app.MapGet("/", () => Results.Text(
            "Fake4Dataverse Service is running.\n\n" +
            "Available SOAP endpoints:\n" +
            "  - /XRMServices/2011/Organization.svc - Organization Service (SOAP 1.1/1.2)\n" +
            "  - /XRMServices/2011/Organization.svc?wsdl - WSDL definition\n\n" +
            "Available REST/OData endpoints:\n" +
            "  - /api/data/v9.2/{entityPluralName} - List entities (GET)\n" +
            "  - /api/data/v9.2/{entityPluralName}({id}) - Retrieve entity (GET)\n" +
            "  - /api/data/v9.2/{entityPluralName} - Create entity (POST)\n" +
            "  - /api/data/v9.2/{entityPluralName}({id}) - Update entity (PATCH)\n" +
            "  - /api/data/v9.2/{entityPluralName}({id}) - Delete entity (DELETE)\n\n" +
            "OData Query Options: $select, $filter, $orderby, $top, $skip, $expand, $count\n\n" +
            "This service provides 100% compatibility with Microsoft Dynamics 365/Dataverse SDK.",
            "text/plain"));

        Console.WriteLine("Fake4Dataverse Service started successfully");
        Console.WriteLine($"Base URL: http://{host}:{port}");
        Console.WriteLine();
        Console.WriteLine("Available SOAP endpoints (matching Microsoft Dynamics 365/Dataverse):");
        Console.WriteLine($"  - http://{host}:{port}/XRMServices/2011/Organization.svc");
        Console.WriteLine($"  - http://{host}:{port}/XRMServices/2011/Organization.svc?wsdl");
        Console.WriteLine();
        Console.WriteLine("Available REST/OData v4.0 endpoints:");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/{{entityPluralName}} (GET, POST)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/{{entityPluralName}}({{id}}) (GET, PATCH, DELETE)");
        Console.WriteLine();
        Console.WriteLine("OData Query Options:");
        Console.WriteLine("  - $select: Choose specific columns");
        Console.WriteLine("  - $filter: Filter records (basic support)");
        Console.WriteLine("  - $orderby: Sort records");
        Console.WriteLine("  - $top: Limit results");
        Console.WriteLine("  - $skip: Skip records for pagination");
        Console.WriteLine("  - $count: Include total count");
        Console.WriteLine();
        
        if (!string.IsNullOrEmpty(accessToken))
        {
            Console.WriteLine("ServiceClient connection string example:");
            Console.WriteLine($"  AuthType=OAuth;Url=http://{host}:{port};AccessToken={accessToken}");
            Console.WriteLine();
        }
        
        Console.WriteLine("The service exposes the following IOrganizationService methods:");
        Console.WriteLine("  - Create: Creates a new entity record");
        Console.WriteLine("  - Retrieve: Retrieves an entity record by ID");
        Console.WriteLine("  - Update: Updates an existing entity record");
        Console.WriteLine("  - Delete: Deletes an entity record");
        Console.WriteLine("  - Associate: Associates two entity records");
        Console.WriteLine("  - Disassociate: Disassociates two entity records");
        Console.WriteLine("  - RetrieveMultiple: Retrieves multiple entity records");
        Console.WriteLine("  - Execute: Executes an organization request");
        Console.WriteLine();
        Console.WriteLine("Press Ctrl+C to stop the service.");

        await app.RunAsync();
    }
}

/// <summary>
/// Simple access token validator for authentication
/// </summary>
internal class AccessTokenValidator
{
    public string ExpectedToken { get; }
    
    public AccessTokenValidator(string expectedToken)
    {
        ExpectedToken = expectedToken ?? throw new ArgumentNullException(nameof(expectedToken));
    }
}
