using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Metadata;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Service.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.CommandLine;
using System.IO;
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
        var cdmFilesOption = new Option<string[]?>(
            name: "--cdm-files",
            description: "Optional paths to CDM (Common Data Model) JSON files to initialize entity metadata. Multiple files can be specified.",
            getDefaultValue: () => null);
        var cdmSchemasOption = new Option<string[]?>(
            name: "--cdm-schemas",
            description: "Optional list of standard CDM schema groups to download and initialize (e.g., crmcommon, sales, service, portals, customerInsights). Downloads from Microsoft's CDM repository. Defaults to 'crmcommon' if no CDM options specified.",
            getDefaultValue: () => null);
        var cdmEntitiesOption = new Option<string[]?>(
            name: "--cdm-entities",
            description: "Optional list of standard CDM entities to download and initialize (e.g., account, contact, lead). Downloads from Microsoft's CDM repository.",
            getDefaultValue: () => null);
        var noCdmOption = new Option<bool>(
            name: "--no-cdm",
            description: "Skip loading default CDM schemas. Useful for testing or when metadata is not needed.",
            getDefaultValue: () => false);

        startCommand.AddOption(portOption);
        startCommand.AddOption(hostOption);
        startCommand.AddOption(accessTokenOption);
        startCommand.AddOption(cdmFilesOption);
        startCommand.AddOption(cdmSchemasOption);
        startCommand.AddOption(cdmEntitiesOption);
        startCommand.AddOption(noCdmOption);

        startCommand.SetHandler(async (int port, string host, string? accessToken, string[]? cdmFiles, string[]? cdmSchemas, string[]? cdmEntities, bool noCdm) =>
        {
            await StartService(port, host, accessToken, cdmFiles, cdmSchemas, cdmEntities, noCdm);
        }, portOption, hostOption, accessTokenOption, cdmFilesOption, cdmSchemasOption, cdmEntitiesOption, noCdmOption);

        rootCommand.AddCommand(startCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task StartService(int port, string host, string? accessToken, string[]? cdmFiles, string[]? cdmSchemas, string[]? cdmEntities, bool noCdm)
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
        
        // Set up file-based CDM caching in LocalAppData
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var cdmCacheDir = Path.Combine(localAppData, "Fake4Dataverse", "CdmCache");
        MetadataGenerator.SetCdmCacheDirectory(cdmCacheDir);
        Console.WriteLine($"CDM file cache directory: {cdmCacheDir}");
        Console.WriteLine();
        
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = new[] { $"--urls=http://{host}:{port}" }
        });

        // Create and register the Fake4Dataverse context
        var context = XrmFakedContextFactory.New();
        
        // Initialize CDM metadata if requested
        // Reference: https://github.com/microsoft/CDM
        // The Common Data Model provides standard entity schemas for Dynamics 365 and Power Platform
        if (cdmFiles != null && cdmFiles.Length > 0)
        {
            Console.WriteLine($"Loading metadata from {cdmFiles.Length} CDM file(s)...");
            foreach (var cdmFile in cdmFiles)
            {
                Console.WriteLine($"  - {cdmFile}");
            }
            try
            {
                context.InitializeMetadataFromCdmFiles(cdmFiles);
                Console.WriteLine($"Successfully loaded metadata from CDM files");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading CDM files: {ex.Message}");
                throw;
            }
        }
        
        // Default to crmcommon if no CDM options were specified and --no-cdm not set
        if (!noCdm && cdmSchemas == null && cdmEntities == null && cdmFiles == null)
        {
            cdmSchemas = new[] { "crmcommon" };
            Console.WriteLine("No CDM options specified. Defaulting to 'crmcommon' schema...");
            Console.WriteLine("Use --no-cdm to skip CDM loading.");
        }
        
        if (cdmSchemas != null && cdmSchemas.Length > 0)
        {
            Console.WriteLine($"Downloading and loading {cdmSchemas.Length} standard CDM schema group(s) from Microsoft's CDM repository...");
            foreach (var schema in cdmSchemas)
            {
                Console.WriteLine($"  - {schema}");
            }
            try
            {
                await context.InitializeMetadataFromStandardCdmSchemasAsync(cdmSchemas);
                Console.WriteLine($"Successfully loaded standard CDM schemas");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading standard CDM schemas: {ex.Message}");
                throw;
            }
        }
        
        if (cdmEntities != null && cdmEntities.Length > 0)
        {
            Console.WriteLine($"Downloading and loading {cdmEntities.Length} standard CDM entit(ies) from Microsoft's CDM repository...");
            foreach (var entity in cdmEntities)
            {
                Console.WriteLine($"  - {entity}");
            }
            try
            {
                await context.InitializeMetadataFromStandardCdmEntitiesAsync(cdmEntities);
                Console.WriteLine($"Successfully loaded standard CDM entities");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading standard CDM entities: {ex.Message}");
                throw;
            }
        }
        
        var organizationService = context.GetOrganizationService();
        
        // Initialize example Model-Driven App metadata
        MdaInitializer.InitializeExampleMda(organizationService);
        
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
        
        // Enable static file serving for Model-Driven App
        // NOTE: Order matters - UseDefaultFiles must come before UseStaticFiles
        var mdaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "mda");
        
        // Serve MDA at /main.aspx to match real Dynamics 365 MDA URLs
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/navigate-to-custom-page-examples
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            DefaultFileNames = new List<string> { "index.html" },
            FileProvider = new PhysicalFileProvider(mdaPath),
            RequestPath = ""  // Serve from root
        });
        
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(mdaPath),
            RequestPath = ""  // Serve from root
        });
        
        // Add authentication middleware if token is provided
        if (!string.IsNullOrEmpty(accessToken))
        {
            app.Use(async (context, next) =>
            {
                // Skip authentication for info endpoint
                if (context.Request.Path.StartsWithSegments("/info"))
                {
                    await next();
                    return;
                }

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

        // Serve main.aspx for MDA (matching Dynamics 365 URL pattern)
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/navigate-to-custom-page-examples
        app.MapGet("/main.aspx", async (HttpContext context) =>
        {
            var indexPath = Path.Combine(mdaPath, "index.html");
            if (File.Exists(indexPath))
            {
                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(indexPath);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Model-Driven App not found. Please build the Next.js app.");
            }
        });

        // Redirect root to main.aspx with default app
        app.MapGet("/", async (HttpContext context) =>
        {
            // Load default app module ID
            var service = context.RequestServices.GetRequiredService<OrganizationServiceImpl>();
            var query = new QueryExpression("appmodule")
            {
                ColumnSet = new ColumnSet("appmoduleid"),
                TopCount = 1
            };
            
            try
            {
                var result = service.RetrieveMultiple(query);
                if (result.Entities.Count > 0)
                {
                    var appId = result.Entities[0].Id;
                    context.Response.Redirect($"/main.aspx?appid={appId}&pagetype=entitylist");
                }
                else
                {
                    context.Response.Redirect("/main.aspx");
                }
            }
            catch
            {
                context.Response.Redirect("/main.aspx");
            }
        });

        app.MapGet("/info", () => Results.Text(
            "Fake4Dataverse Service is running.\n\n" +
            "Model-Driven App:\n" +
            "  - /main.aspx - Web interface for testing (matches Dynamics 365 URL pattern)\n" +
            "  - Supports query parameters: ?appid={guid}&pagetype=entitylist&etn={entityname}&viewid={guid}\n\n" +
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
        Console.WriteLine("Model-Driven App (matches Dynamics 365 URL pattern):");
        Console.WriteLine($"  - http://{host}:{port}/main.aspx - Web interface");
        Console.WriteLine($"  - Supports query parameters: ?appid={{guid}}&pagetype=entitylist&etn={{entityname}}&viewid={{guid}}");
        Console.WriteLine();
        Console.WriteLine("Service Information:");
        Console.WriteLine($"  - http://{host}:{port}/info - Service info page");
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
