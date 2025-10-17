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
using Microsoft.Extensions.Hosting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.CommandLine;
using System.IO;
using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

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
            description: "Optional list of standard CDM schema groups to download and initialize. Downloads from Microsoft's CDM repository. Defaults to 'crmcommon' if no CDM options specified. Available: crmcommon, sales, service, marketing, fieldservice, projectservice, portals, customerinsights, linkedinleadgen, socialengagement, gamification.",
            getDefaultValue: () => null);
        var cdmEntitiesOption = new Option<string[]?>(
            name: "--cdm-entities",
            description: "Optional list of standard CDM entities to download and initialize. Downloads from Microsoft's CDM repository. Examples: account, contact, lead, systemuser, team, businessunit, organization, email, phonecall, appointment, task, opportunity, quote, order, invoice, incident, case, contract, campaign, workorder. For a full list of available entities, see: https://github.com/microsoft/CDM/blob/master/entity/reference.md",
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
        
        // When using port 0 (auto-assign), Kestrel requires using IP address instead of 'localhost'
        // Reference: https://github.com/dotnet/aspnetcore/issues/29235
        var bindHost = (port == 0 && host == "localhost") ? "127.0.0.1" : host;
        
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = new[] { $"--urls=http://{bindHost}:{port}" }
        });

        // Create and register the Fake4Dataverse context with validation enabled
        // System entity metadata is automatically loaded from embedded resources in Core
        var context = XrmFakedContextFactory.New();
        
        // Load system entity metadata (solution, appmodule, sitemap, etc.) from embedded resources in Core
        // These system entities are required for Model-Driven App functionality
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/create-model-driven-app-using-code
        Console.WriteLine("Loading system entity metadata from embedded resources...");
        try
        {
            context.InitializeSystemEntityMetadata();
            Console.WriteLine("Successfully loaded system entity metadata");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load system entity metadata: {ex.Message}");
            // Continue - some functionality may not work but service can still start
        }
        Console.WriteLine();
        
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
        if (!noCdm && (cdmSchemas == null || cdmSchemas.Length == 0) && (cdmEntities == null || cdmEntities.Length == 0) && (cdmFiles == null || cdmFiles.Length == 0))
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
                
                // Log loaded entities for verification
                var metadata = context.CreateMetadataQuery();
                var entityCount = metadata.Count();
                Console.WriteLine($"Total entities loaded: {entityCount}");
                
                // Check for key entities
                var accountLoaded = metadata.Any(e => e.LogicalName == "account");
                var contactLoaded = metadata.Any(e => e.LogicalName == "contact");
                var leadLoaded = metadata.Any(e => e.LogicalName == "lead");
                
                if (accountLoaded) Console.WriteLine("  ✓ account entity loaded");
                if (contactLoaded) Console.WriteLine("  ✓ contact entity loaded");
                if (leadLoaded) Console.WriteLine("  ✓ lead entity loaded");
                
                if (!accountLoaded || !contactLoaded)
                {
                    Console.WriteLine("  ⚠ Warning: Core entities (account, contact) were not loaded from crmcommon schema");
                }
                
                // Enable auditing for all loaded entities
                // By default, CDM metadata may have IsAuditEnabled set to false or null
                // Enable it for all entities to support audit history tracking
                Console.WriteLine("Enabling auditing for all loaded entities...");
                int enabledCount = 0;
                foreach (var entityMetadata in metadata)
                {
                    // IsAuditEnabled might be null - in which case auditing is allowed by default
                    // But explicitly setting it to true ensures consistent behavior
                    if (entityMetadata.IsAuditEnabled != null)
                    {
                        entityMetadata.IsAuditEnabled.Value = true;
                        enabledCount++;
                    }
                    
                    // Also enable auditing for all attributes
                    if (entityMetadata.Attributes != null)
                    {
                        foreach (var attribute in entityMetadata.Attributes)
                        {
                            if (attribute.IsAuditEnabled != null)
                            {
                                attribute.IsAuditEnabled.Value = true;
                            }
                        }
                    }
                }
                Console.WriteLine($"Auditing enabled for {enabledCount} entities (out of {entityCount} total)");
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
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/create-model-driven-app-using-code
        // System entity metadata must be loaded before this call
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

                // Add EDM model for metadata endpoints
                options.AddRouteComponents("api/data/v9.2", GetMetadataEdmModel());
            });

        var app = builder.Build();
        
        // In development mode, requests to SPA routes will be automatically proxied to the Next.js dev server
        // The Microsoft.AspNetCore.SpaProxy package handles this automatically based on SpaProxyServerUrl configuration
        // In production, serve pre-built static files from wwwroot/mda
        
        // Production mode: serve static files from wwwroot/mda
        // Use AppContext.BaseDirectory to get the application's base directory (where the DLL is)
        // This ensures the path works correctly whether running with 'dotnet run' or 'dotnet run --no-build'
        var mdaPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "mda");
        
        // Only configure static file serving if the MDA path exists
        // This allows the service to run even without MDA files (for testing)
        if (Directory.Exists(mdaPath) && !app.Environment.IsDevelopment())
        {
            // Serve MDA at /main.aspx to match real Dynamics 365 MDA URLs
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/navigate-to-custom-page-examples
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = new List<string> { "index.html" },
                FileProvider = new PhysicalFileProvider(mdaPath),
                RequestPath = ""  // Serve from root
            });
            
            // Middleware to serve .html files for routes without extensions (for Next.js static export)
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value;
                if (!string.IsNullOrEmpty(path) && path != "/" && !Path.HasExtension(path))
                {
                    var htmlFile = path.TrimStart('/') + ".html";
                    var htmlPath = Path.Combine(mdaPath, htmlFile);
                    if (File.Exists(htmlPath))
                    {
                        context.Request.Path = "/" + htmlFile;
                    }
                }
                await next();
            });
            
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(mdaPath),
                RequestPath = ""  // Serve from root
            });
        }
        
        // Add authentication middleware if token is provided
        // This middleware runs BEFORE endpoint execution
        if (!string.IsNullOrEmpty(accessToken))
        {
            app.Use(async (context, next) =>
            {
                // Skip authentication for health check and info endpoints
                if (context.Request.Path.StartsWithSegments("/health") ||
                    context.Request.Path.StartsWithSegments("/info"))
                {
                    await next();
                    return;
                }

                // Skip authentication for static files
                if (context.Request.Path.StartsWithSegments("/_next") ||
                    context.Request.Path.Value?.Contains('.') == true && 
                    !context.Request.Path.StartsWithSegments("/api"))
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
                context.Response.Headers.Append("WWW-Authenticate", "Bearer");
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

        // Health check endpoint for testing - returns 200 OK when service is fully ready
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

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
            "Metadata REST/OData endpoints:\n" +
            "  - /api/data/v9.2/EntityDefinitions - List entity metadata (GET)\n" +
            "  - /api/data/v9.2/EntityDefinitions({id}) - Get entity metadata by MetadataId (GET)\n" +
            "  - /api/data/v9.2/EntityDefinitions(LogicalName='{name}') - Get entity metadata by name (GET)\n" +
            "  - /api/data/v9.2/AttributeDefinitions - List attribute metadata (GET)\n" +
            "  - /api/data/v9.2/AttributeDefinitions({id}) - Get attribute metadata by MetadataId (GET)\n" +
            "  - /api/data/v9.2/RelationshipDefinitions - List relationship metadata (GET)\n" +
            "  - /api/data/v9.2/RelationshipDefinitions({id}) - Get relationship metadata by MetadataId (GET)\n" +
            "  - /api/data/v9.2/OptionSetDefinitions - List local option set metadata (GET)\n" +
            "  - /api/data/v9.2/OptionSetDefinitions({id}) - Get local option set metadata by MetadataId (GET)\n" +
            "  - /api/data/v9.2/GlobalOptionSetDefinitions - List global option set metadata (GET)\n" +
            "  - /api/data/v9.2/GlobalOptionSetDefinitions({id}) - Get global option set metadata by MetadataId (GET)\n" +
            "  - /api/data/v9.2/EntityKeyDefinitions - List entity key metadata (GET)\n" +
            "  - /api/data/v9.2/EntityKeyDefinitions({id}) - Get entity key metadata by MetadataId (GET)\n" +
            "  - /api/data/v9.2/$metadata - OData service document (EDMX/CSDL)\n\n" +
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
        Console.WriteLine("Metadata REST/OData endpoints:");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/EntityDefinitions (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/EntityDefinitions({{id}}) (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/EntityDefinitions(LogicalName='{{name}}') (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/AttributeDefinitions (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/AttributeDefinitions({{id}}) (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/RelationshipDefinitions (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/RelationshipDefinitions({{id}}) (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/OptionSetDefinitions (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/OptionSetDefinitions({{id}}) (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/GlobalOptionSetDefinitions (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/GlobalOptionSetDefinitions({{id}}) (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/EntityKeyDefinitions (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/EntityKeyDefinitions({{id}}) (GET)");
        Console.WriteLine($"  - http://{host}:{port}/api/data/v9.2/$metadata - OData service document (EDMX/CSDL)");
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

        // Start the application
        await app.StartAsync();
        
        // After starting, get the actual listening URLs (important when port 0 is used for auto-assignment)
        var actualUrls = app.Urls;
        if (actualUrls.Count > 0)
        {
            var actualUrl = actualUrls.First();
            Console.WriteLine();
            Console.WriteLine($"ACTUAL_URL: {actualUrl}");
            Console.WriteLine();
        }
        
        // Keep the application running
        var tcs = new TaskCompletionSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            tcs.TrySetResult();
        };
        await tcs.Task;
        
        // Shutdown
        await app.StopAsync();
    }

    /// <summary>
    /// Builds the EDM model for OData metadata endpoints.
    /// Reference: https://learn.microsoft.com/en-us/odata/webapi-8/fundamentals/edm-model-builder
    /// 
    /// The EDM (Entity Data Model) defines the structure of metadata exposed through OData.
    /// This includes entity metadata types like EntityDefinitions, AttributeDefinitions, etc.
    /// 
    /// Note: This model is for METADATA queries only (EntityDefinitions, AttributeDefinitions, etc.).
    /// Data entity queries (accounts, contacts, etc.) are handled separately by ODataEntityController
    /// which doesn't use this EDM model.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-service-documents
    /// The $metadata endpoint returns this model as EDMX/CSDL.
    /// </summary>
    private static IEdmModel GetMetadataEdmModel()
    {
        var builder = new ODataConventionModelBuilder();
        
        // EntityDefinitions - Entity metadata
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        // EntityDefinitions corresponds to EntityMetadata in the SDK and supports full OData querying
        // via [EnableQuery] on MetadataController methods
        var entityDefSet = builder.EntitySet<Microsoft.Xrm.Sdk.Metadata.EntityMetadata>("EntityDefinitions");
        entityDefSet.EntityType.HasKey(e => e.MetadataId);
        
        // Configure navigation to Attributes collection
        // This enables $expand=Attributes on EntityDefinitions queries
        // Reference: https://learn.microsoft.com/en-us/odata/webapi-8/fundamentals/navigation-property
        entityDefSet.EntityType.HasMany(em => em.Attributes);
        
        // AttributeDefinitions - Attribute metadata
        var attrDefSet = builder.EntitySet<Microsoft.Xrm.Sdk.Metadata.AttributeMetadata>("AttributeDefinitions");
        attrDefSet.EntityType.HasKey(a => a.MetadataId);
        
        // RelationshipDefinitions - Relationship metadata
        var relDefSet = builder.EntitySet<Microsoft.Xrm.Sdk.Metadata.RelationshipMetadataBase>("RelationshipDefinitions");
        relDefSet.EntityType.HasKey(r => r.MetadataId);
        
        // OptionSetDefinitions - Local option sets
        var optSetDefSet = builder.EntitySet<Microsoft.Xrm.Sdk.Metadata.OptionSetMetadata>("OptionSetDefinitions");
        optSetDefSet.EntityType.HasKey(o => o.MetadataId);
        
        // GlobalOptionSetDefinitions - Global option sets
        // Note: This reuses the same OptionSetMetadata type, so the key configuration is inherited
        builder.EntitySet<Microsoft.Xrm.Sdk.Metadata.OptionSetMetadata>("GlobalOptionSetDefinitions");
        
        // EntityKeyDefinitions - Alternate keys
        var entityKeyDefSet = builder.EntitySet<Microsoft.Xrm.Sdk.Metadata.EntityKeyMetadata>("EntityKeyDefinitions");
        entityKeyDefSet.EntityType.HasKey(k => k.MetadataId);
        
        return builder.GetEdmModel();
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
