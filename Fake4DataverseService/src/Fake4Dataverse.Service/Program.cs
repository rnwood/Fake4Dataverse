using Fake4Dataverse;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Service.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using System.CommandLine;

namespace Fake4Dataverse.Service;

/// <summary>
/// CLI service host for Fake4Dataverse IOrganizationService.
/// This service provides a gRPC endpoint that exposes a fake IOrganizationService
/// backed by Fake4Dataverse, using 100% Microsoft Dataverse SDK types.
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
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Fake4Dataverse CLI Service - Host a fake IOrganizationService for testing and development");

        var startCommand = new Command("start", "Start the Fake4Dataverse service");
        var portOption = new Option<int>(
            name: "--port",
            description: "The port to listen on",
            getDefaultValue: () => 5000);
        var hostOption = new Option<string>(
            name: "--host",
            description: "The host to bind to",
            getDefaultValue: () => "localhost");

        startCommand.AddOption(portOption);
        startCommand.AddOption(hostOption);

        startCommand.SetHandler(async (int port, string host) =>
        {
            await StartService(port, host);
        }, portOption, hostOption);

        rootCommand.AddCommand(startCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task StartService(int port, string host)
    {
        Console.WriteLine($"Starting Fake4Dataverse Service on {host}:{port}...");
        
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = new[] { $"--urls=http://{host}:{port}" }
        });

        // Configure services
        builder.Services.AddGrpc();

        // Create and register the Fake4Dataverse context
        var context = XrmFakedContextFactory.New();
        var organizationService = context.GetOrganizationService();
        
        builder.Services.AddSingleton<IOrganizationService>(organizationService);

        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.MapGrpcService<OrganizationServiceImpl>();
        
        app.MapGet("/", () => "Fake4Dataverse Service is running. Use gRPC clients to connect.");

        Console.WriteLine($"Fake4Dataverse Service started successfully on {host}:{port}");
        Console.WriteLine("Press Ctrl+C to stop the service.");
        Console.WriteLine();
        Console.WriteLine("Available services:");
        Console.WriteLine("  - OrganizationService (gRPC)");
        Console.WriteLine();
        Console.WriteLine("The service exposes the following IOrganizationService methods:");
        Console.WriteLine("  - Create: Creates a new entity record");
        Console.WriteLine("  - Retrieve: Retrieves an entity record by ID");
        Console.WriteLine("  - Update: Updates an existing entity record");
        Console.WriteLine("  - Delete: Deletes an entity record");
        Console.WriteLine("  - Associate: Associates two entity records");
        Console.WriteLine("  - Disassociate: Disassociates two entity records");
        Console.WriteLine("  - RetrieveMultiple: Retrieves multiple entity records");
        Console.WriteLine("  - Execute: Executes an organization request");

        await app.RunAsync();
    }
}
