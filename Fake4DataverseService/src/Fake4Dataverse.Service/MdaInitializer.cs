using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.Service;

/// <summary>
/// Helper class for initializing Model-Driven App (MDA) metadata
/// Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/create-model-driven-app-using-code
/// </summary>
public static class MdaInitializer
{
    /// <summary>
    /// Initialize example MDA metadata (appmodule, sitemap, systemview)
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/create-site-map-app
    /// </summary>
    public static void InitializeExampleMda(IOrganizationService service)
    {
        Console.WriteLine("Initializing example Model-Driven App metadata...");
        
        // Create AppModule
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/appmodule
        var appModuleId = Guid.NewGuid();
        var appModule = new Entity("appmodule")
        {
            Id = appModuleId,
            ["name"] = "Fake4Dataverse Example App",
            ["uniquename"] = "fake4dataverse_example",
            ["description"] = "Example Model-Driven App for testing",
            ["clienttype"] = 4, // Unified Interface
            ["statecode"] = 0,
            ["statuscode"] = 1
        };
        service.Create(appModule);
        Console.WriteLine($"  Created AppModule: {appModuleId}");
        
        // Create SiteMap with navigation structure
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/create-site-map-app
        var sitemapId = Guid.NewGuid();
        var sitemapXml = CreateExampleSiteMapXml();
        var sitemap = new Entity("sitemap")
        {
            Id = sitemapId,
            ["sitemapname"] = "Example Sitemap",
            ["sitemapnameunique"] = "fake4dataverse_example_sitemap",
            ["appmoduleid"] = new EntityReference("appmodule", appModuleId),
            ["sitemapxml"] = sitemapXml,
            ["statecode"] = 0,
            ["statuscode"] = 1
        };
        service.Create(sitemap);
        Console.WriteLine($"  Created SiteMap: {sitemapId}");
        
        // Create some sample data for testing
        CreateSampleData(service);
        
        Console.WriteLine("Model-Driven App metadata initialized successfully!");
    }
    
    private static string CreateExampleSiteMapXml()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<SiteMap>
  <Area Id=""area_sales"" Title=""Sales"" Icon=""mdi-cash"">
    <Group Id=""group_customers"" Title=""Customers"">
      <SubArea Id=""subarea_accounts"" Title=""Accounts"" Entity=""account"" Icon=""mdi-domain"" />
      <SubArea Id=""subarea_contacts"" Title=""Contacts"" Entity=""contact"" Icon=""mdi-account"" />
    </Group>
    <Group Id=""group_sales"" Title=""Sales"">
      <SubArea Id=""subarea_opportunities"" Title=""Opportunities"" Entity=""opportunity"" Icon=""mdi-currency-usd"" />
      <SubArea Id=""subarea_leads"" Title=""Leads"" Entity=""lead"" Icon=""mdi-account-star"" />
    </Group>
  </Area>
  <Area Id=""area_service"" Title=""Service"" Icon=""mdi-lifebuoy"">
    <Group Id=""group_cases"" Title=""Cases"">
      <SubArea Id=""subarea_cases"" Title=""Cases"" Entity=""incident"" Icon=""mdi-ticket"" />
    </Group>
  </Area>
</SiteMap>";
    }
    
    private static void CreateSampleData(IOrganizationService service)
    {
        Console.WriteLine("  Creating sample data...");
        
        // Create sample accounts
        for (int i = 1; i <= 5; i++)
        {
            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = $"Sample Account {i}",
                ["accountnumber"] = $"ACC-{i:000}",
                ["revenue"] = new Money(100000 * i),
                ["numberofemployees"] = 50 * i,
                ["statecode"] = 0,
                ["statuscode"] = 1
            };
            service.Create(account);
        }
        Console.WriteLine($"    Created 5 sample accounts");
        
        // Create sample contacts
        for (int i = 1; i <= 10; i++)
        {
            var contact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = $"First{i}",
                ["lastname"] = $"Last{i}",
                ["emailaddress1"] = $"user{i}@example.com",
                ["telephone1"] = $"+1-555-{i:000}-0000",
                ["statecode"] = 0,
                ["statuscode"] = 1
            };
            service.Create(contact);
        }
        Console.WriteLine($"    Created 10 sample contacts");
        
        // Create sample opportunities
        for (int i = 1; i <= 3; i++)
        {
            var opportunity = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["name"] = $"Sample Opportunity {i}",
                ["estimatedvalue"] = new Money(50000 * i),
                ["estimatedclosedate"] = DateTime.UtcNow.AddMonths(i),
                ["statecode"] = 0,
                ["statuscode"] = 1
            };
            service.Create(opportunity);
        }
        Console.WriteLine($"    Created 3 sample opportunities");
    }
}
