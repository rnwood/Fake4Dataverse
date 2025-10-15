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
        
        // Create Active solution (special Microsoft solution containing all components)
        // Reference: https://learn.microsoft.com/en-us/power-platform/alm/solution-concepts-alm#active-solution
        CreateActiveSolution(service);
        
        // Create AppModule
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/appmodule
        var appModuleId = Guid.NewGuid();
        var appModule = new Entity("appmodule")
        {
            Id = appModuleId,
            ["name"] = "Fake4Dataverse Example App",
            ["uniquename"] = "fake4dataverse_example",
            ["description"] = "Example Model-Driven App for testing",
            ["clienttype"] = 4 // Unified Interface
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
            ["sitemapxml"] = sitemapXml
        };
        service.Create(sitemap);
        Console.WriteLine($"  Created SiteMap: {sitemapId}");
        
        // Create saved queries (system views) for entities
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
        CreateSystemViews(service, appModuleId);
        
        // Create system forms for entities
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
        CreateSystemForms(service, appModuleId);
        
        // Create web resources (form scripts)
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource
        CreateWebResources(service);
        
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
                ["numberofemployees"] = 50 * i
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
                ["telephone1"] = $"+1-555-{i:000}-0000"
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
                ["estimatedclosedate"] = DateTime.UtcNow.AddMonths(i)
            };
            service.Create(opportunity);
        }
        Console.WriteLine($"    Created 3 sample opportunities");
    }
    
    /// <summary>
    /// Create system views (SavedQuery) for entities
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/appmodulecomponent
    /// </summary>
    private static void CreateSystemViews(IOrganizationService service, Guid appModuleId)
    {
        Console.WriteLine("  Creating system views...");
        
        // Account views
        CreateAccountViews(service, appModuleId);
        
        // Contact views  
        CreateContactViews(service, appModuleId);
        
        // Opportunity views
        CreateOpportunityViews(service, appModuleId);
        
        Console.WriteLine($"    Created system views for Account, Contact, and Opportunity");
    }
    
    private static void CreateAccountViews(IOrganizationService service, Guid appModuleId)
    {
        // Active Accounts view
        var activeAccountsView = new Entity("savedquery")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Active Accounts",
            ["returnedtypecode"] = "account",
            ["fetchxml"] = @"<fetch version='1.0' output-format='xml-platform' mapping='logical'>
  <entity name='account'>
    <attribute name='accountid' />
    <attribute name='name' />
    <attribute name='accountnumber' />
    <attribute name='revenue' />
    <attribute name='numberofemployees' />
    <attribute name='createdon' />
    <order attribute='name' descending='false' />
    <filter type='and'>
      <condition attribute='statecode' operator='eq' value='0' />
    </filter>
  </entity>
</fetch>",
            ["layoutxml"] = @"<grid name='resultset' object='1' jump='name' select='1' icon='1' preview='1'>
  <row name='result' id='accountid'>
    <cell name='name' width='200' />
    <cell name='accountnumber' width='100' />
    <cell name='revenue' width='100' />
    <cell name='numberofemployees' width='100' />
    <cell name='createdon' width='150' />
  </row>
</grid>",
            ["querytype"] = 0, // 0 = Public View
            ["isdefault"] = true,
            ["iscustomizable"] = true
        };
        service.Create(activeAccountsView);
        CreateAppModuleComponent(service, appModuleId, activeAccountsView.Id, 26); // 26 = SavedQuery
        
        // All Accounts view
        var allAccountsView = new Entity("savedquery")
        {
            Id = Guid.NewGuid(),
            ["name"] = "All Accounts",
            ["returnedtypecode"] = "account",
            ["fetchxml"] = @"<fetch version='1.0' output-format='xml-platform' mapping='logical'>
  <entity name='account'>
    <attribute name='accountid' />
    <attribute name='name' />
    <attribute name='accountnumber' />
    <attribute name='revenue' />
    <attribute name='primarycontactid' />
    <order attribute='name' descending='false' />
  </entity>
</fetch>",
            ["layoutxml"] = @"<grid name='resultset' object='1' jump='name' select='1' icon='1' preview='1'>
  <row name='result' id='accountid'>
    <cell name='name' width='200' />
    <cell name='accountnumber' width='100' />
    <cell name='revenue' width='100' />
  </row>
</grid>",
            ["querytype"] = 0,
            ["isdefault"] = false,
            ["iscustomizable"] = true
        };
        service.Create(allAccountsView);
        CreateAppModuleComponent(service, appModuleId, allAccountsView.Id, 26);
    }
    
    private static void CreateContactViews(IOrganizationService service, Guid appModuleId)
    {
        // Active Contacts view
        var activeContactsView = new Entity("savedquery")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Active Contacts",
            ["returnedtypecode"] = "contact",
            ["fetchxml"] = @"<fetch version='1.0' output-format='xml-platform' mapping='logical'>
  <entity name='contact'>
    <attribute name='contactid' />
    <attribute name='firstname' />
    <attribute name='lastname' />
    <attribute name='emailaddress1' />
    <attribute name='telephone1' />
    <order attribute='lastname' descending='false' />
    <filter type='and'>
      <condition attribute='statecode' operator='eq' value='0' />
    </filter>
  </entity>
</fetch>",
            ["layoutxml"] = @"<grid name='resultset' object='2' jump='fullname' select='1' icon='1' preview='1'>
  <row name='result' id='contactid'>
    <cell name='firstname' width='100' />
    <cell name='lastname' width='100' />
    <cell name='emailaddress1' width='150' />
    <cell name='telephone1' width='120' />
  </row>
</grid>",
            ["querytype"] = 0,
            ["isdefault"] = true,
            ["iscustomizable"] = true
        };
        service.Create(activeContactsView);
        CreateAppModuleComponent(service, appModuleId, activeContactsView.Id, 26);
        
        // All Contacts view
        var allContactsView = new Entity("savedquery")
        {
            Id = Guid.NewGuid(),
            ["name"] = "All Contacts",
            ["returnedtypecode"] = "contact",
            ["fetchxml"] = @"<fetch version='1.0' output-format='xml-platform' mapping='logical'>
  <entity name='contact'>
    <attribute name='contactid' />
    <attribute name='firstname' />
    <attribute name='lastname' />
    <attribute name='emailaddress1' />
    <attribute name='jobtitle' />
    <order attribute='lastname' descending='false' />
  </entity>
</fetch>",
            ["layoutxml"] = @"<grid name='resultset' object='2' jump='fullname' select='1' icon='1' preview='1'>
  <row name='result' id='contactid'>
    <cell name='firstname' width='100' />
    <cell name='lastname' width='100' />
    <cell name='emailaddress1' width='150' />
  </row>
</grid>",
            ["querytype"] = 0,
            ["isdefault"] = false,
            ["iscustomizable"] = true
        };
        service.Create(allContactsView);
        CreateAppModuleComponent(service, appModuleId, allContactsView.Id, 26);
    }
    
    private static void CreateOpportunityViews(IOrganizationService service, Guid appModuleId)
    {
        // Open Opportunities view
        var openOpportunitiesView = new Entity("savedquery")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Open Opportunities",
            ["returnedtypecode"] = "opportunity",
            ["fetchxml"] = @"<fetch version='1.0' output-format='xml-platform' mapping='logical'>
  <entity name='opportunity'>
    <attribute name='opportunityid' />
    <attribute name='name' />
    <attribute name='estimatedvalue' />
    <attribute name='estimatedclosedate' />
    <attribute name='createdon' />
    <order attribute='estimatedclosedate' descending='false' />
    <filter type='and'>
      <condition attribute='statecode' operator='eq' value='0' />
    </filter>
  </entity>
</fetch>",
            ["layoutxml"] = @"<grid name='resultset' object='3' jump='name' select='1' icon='1' preview='1'>
  <row name='result' id='opportunityid'>
    <cell name='name' width='200' />
    <cell name='estimatedvalue' width='100' />
    <cell name='estimatedclosedate' width='120' />
  </row>
</grid>",
            ["querytype"] = 0,
            ["isdefault"] = true,
            ["iscustomizable"] = true
        };
        service.Create(openOpportunitiesView);
        CreateAppModuleComponent(service, appModuleId, openOpportunitiesView.Id, 26);
    }
    
    /// <summary>
    /// Create AppModuleComponent to link a component (like SavedQuery) to an AppModule
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/appmodulecomponent
    /// </summary>
    private static void CreateAppModuleComponent(IOrganizationService service, Guid appModuleId, Guid componentId, int componentType)
    {
        var appModuleComponent = new Entity("appmodulecomponent")
        {
            Id = Guid.NewGuid(),
            ["appmoduleidunique"] = appModuleId,
            ["objectid"] = componentId,
            ["componenttype"] = componentType // 26 = SavedQuery, 1 = Entity, 60 = SystemForm
        };
        service.Create(appModuleComponent);
    }
    
    /// <summary>
    /// Create system forms for entities
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
    /// Form types: 2=Main, 4=Quick Create, 6=Quick View, 7=Quick View Form, 8=Dialog, 9=Task Flow Form
    /// </summary>
    private static void CreateSystemForms(IOrganizationService service, Guid appModuleId)
    {
        Console.WriteLine("  Creating system forms...");
        
        CreateAccountForm(service, appModuleId);
        CreateContactForm(service, appModuleId);
        CreateOpportunityForm(service, appModuleId);
        
        Console.WriteLine($"    Created system forms for Account, Contact, and Opportunity");
    }
    
    private static void CreateAccountForm(IOrganizationService service, Guid appModuleId)
    {
        var formId = Guid.NewGuid();
        var libraryId = Guid.NewGuid();
        var formXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<form>
  <formLibraries>
    <Library name=""fake4dataverse_account_form_script.js"" libraryUniqueId=""" + libraryId + @""" />
  </formLibraries>
  <events>
    <event name=""onload"" application=""true"" active=""true"">
      <Handler functionName=""OnLoad"" libraryName=""fake4dataverse_account_form_script.js"" handlerUniqueId=""" + Guid.NewGuid() + @""" enabled=""true"" parameters="""" passExecutionContext=""true"" />
    </event>
    <event name=""onsave"" application=""true"" active=""true"">
      <Handler functionName=""OnSave"" libraryName=""fake4dataverse_account_form_script.js"" handlerUniqueId=""" + Guid.NewGuid() + @""" enabled=""true"" parameters="""" passExecutionContext=""true"" />
    </event>
    <event name=""onchange"" application=""true"" active=""true"" attribute=""name"">
      <Handler functionName=""ValidateAccountName"" libraryName=""fake4dataverse_account_form_script.js"" handlerUniqueId=""" + Guid.NewGuid() + @""" enabled=""true"" parameters="""" passExecutionContext=""true"" />
    </event>
  </events>
  <tabs>
    <tab id=""tab_general"" name=""general"" verticallayout=""true"" visible=""true"">
      <labels>
        <label description=""General"" languagecode=""1033"" />
      </labels>
      <columns>
        <column width=""100%"">
          <sections>
            <section id=""section_account_info"" name=""account_information"" visible=""true"">
              <labels>
                <label description=""Account Information"" languagecode=""1033"" />
              </labels>
              <rows>
                <row>
                  <cell id=""accountname"">
                    <labels>
                      <label description=""Account Name"" languagecode=""1033"" />
                    </labels>
                    <control id=""accountname"" classid=""{270BD3DB-D9AF-4782-9025-509E298DEC0A}"" datafieldname=""name"" disabled=""false"" />
                  </cell>
                </row>
                <row>
                  <cell id=""accountnumber"">
                    <labels>
                      <label description=""Account Number"" languagecode=""1033"" />
                    </labels>
                    <control id=""accountnumber"" classid=""{270BD3DB-D9AF-4782-9025-509E298DEC0A}"" datafieldname=""accountnumber"" disabled=""false"" />
                  </cell>
                </row>
                <row>
                  <cell id=""revenue"">
                    <labels>
                      <label description=""Annual Revenue"" languagecode=""1033"" />
                    </labels>
                    <control id=""revenue"" classid=""{533B9E00-756B-4312-95A0-DC888637AC78}"" datafieldname=""revenue"" disabled=""false"" />
                  </cell>
                </row>
                <row>
                  <cell id=""numberofemployees"">
                    <labels>
                      <label description=""Number of Employees"" languagecode=""1033"" />
                    </labels>
                    <control id=""numberofemployees"" classid=""{C3EFE0C3-0EC6-42BE-8349-CBD9079DFD8E}"" datafieldname=""numberofemployees"" disabled=""false"" />
                  </cell>
                </row>
              </rows>
            </section>
          </sections>
        </column>
      </columns>
    </tab>
    <tab id=""tab_details"" name=""details"" verticallayout=""true"" visible=""true"">
      <labels>
        <label description=""Details"" languagecode=""1033"" />
      </labels>
      <columns>
        <column width=""100%"">
          <sections>
            <section id=""section_contact_info"" name=""contact_information"" visible=""true"">
              <labels>
                <label description=""Contact Information"" languagecode=""1033"" />
              </labels>
              <rows>
                <row>
                  <cell id=""telephone1"">
                    <labels>
                      <label description=""Main Phone"" languagecode=""1033"" />
                    </labels>
                    <control id=""telephone1"" classid=""{270BD3DB-D9AF-4782-9025-509E298DEC0A}"" datafieldname=""telephone1"" disabled=""false"" />
                  </cell>
                </row>
                <row>
                  <cell id=""emailaddress1"">
                    <labels>
                      <label description=""Email"" languagecode=""1033"" />
                    </labels>
                    <control id=""emailaddress1"" classid=""{270BD3DB-D9AF-4782-9025-509E298DEC0A}"" datafieldname=""emailaddress1"" disabled=""false"" />
                  </cell>
                </row>
              </rows>
            </section>
          </sections>
        </column>
      </columns>
    </tab>
  </tabs>
</form>";
        
        var accountForm = new Entity("systemform")
        {
            Id = formId,
            ["name"] = "Account Main Form",
            ["objecttypecode"] = "account",
            ["type"] = 2, // Main form
            ["formxml"] = formXml,
            ["isdefault"] = true,
            ["iscustomizable"] = true
        };
        service.Create(accountForm);
        CreateAppModuleComponent(service, appModuleId, formId, 60); // 60 = SystemForm
    }
    
    private static void CreateContactForm(IOrganizationService service, Guid appModuleId)
    {
        var formId = Guid.NewGuid();
        var formXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<form>
  <tabs>
    <tab id=""tab_general"" name=""general"" verticallayout=""true"" visible=""true"">
      <labels>
        <label description=""General"" languagecode=""1033"" />
      </labels>
      <columns>
        <column width=""100%"">
          <sections>
            <section id=""section_contact_info"" name=""contact_information"" visible=""true"">
              <labels>
                <label description=""Contact Information"" languagecode=""1033"" />
              </labels>
              <rows>
                <row>
                  <cell id=""firstname"">
                    <labels>
                      <label description=""First Name"" languagecode=""1033"" />
                    </labels>
                    <control id=""firstname"" classid=""{270BD3DB-D9AF-4782-9025-509E298DEC0A}"" datafieldname=""firstname"" disabled=""false"" />
                  </cell>
                </row>
                <row>
                  <cell id=""lastname"">
                    <labels>
                      <label description=""Last Name"" languagecode=""1033"" />
                    </labels>
                    <control id=""lastname"" classid=""{270BD3DB-D9AF-4782-9025-509E298DEC0A}"" datafieldname=""lastname"" disabled=""false"" />
                  </cell>
                </row>
                <row>
                  <cell id=""emailaddress1"">
                    <labels>
                      <label description=""Email"" languagecode=""1033"" />
                    </labels>
                    <control id=""emailaddress1"" classid=""{270BD3DB-D9AF-4782-9025-509E298DEC0A}"" datafieldname=""emailaddress1"" disabled=""false"" />
                  </cell>
                </row>
                <row>
                  <cell id=""telephone1"">
                    <labels>
                      <label description=""Business Phone"" languagecode=""1033"" />
                    </labels>
                    <control id=""telephone1"" classid=""{270BD3DB-D9AF-4782-9025-509E298DEC0A}"" datafieldname=""telephone1"" disabled=""false"" />
                  </cell>
                </row>
              </rows>
            </section>
          </sections>
        </column>
      </columns>
    </tab>
  </tabs>
</form>";
        
        var contactForm = new Entity("systemform")
        {
            Id = formId,
            ["name"] = "Contact Main Form",
            ["objecttypecode"] = "contact",
            ["type"] = 2,
            ["formxml"] = formXml,
            ["isdefault"] = true,
            ["iscustomizable"] = true
        };
        service.Create(contactForm);
        CreateAppModuleComponent(service, appModuleId, formId, 60);
    }
    
    private static void CreateOpportunityForm(IOrganizationService service, Guid appModuleId)
    {
        var formId = Guid.NewGuid();
        var formXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<form>
  <tabs>
    <tab id=""tab_general"" name=""general"" verticallayout=""true"" visible=""true"">
      <labels>
        <label description=""General"" languagecode=""1033"" />
      </labels>
      <columns>
        <column width=""100%"">
          <sections>
            <section id=""section_opportunity_info"" name=""opportunity_information"" visible=""true"">
              <labels>
                <label description=""Opportunity Information"" languagecode=""1033"" />
              </labels>
              <rows>
                <row>
                  <cell id=""name"">
                    <labels>
                      <label description=""Topic"" languagecode=""1033"" />
                    </labels>
                    <control id=""name"" classid=""{270BD3DB-D9AF-4782-9025-509E298DEC0A}"" datafieldname=""name"" disabled=""false"" />
                  </cell>
                </row>
                <row>
                  <cell id=""estimatedvalue"">
                    <labels>
                      <label description=""Est. Revenue"" languagecode=""1033"" />
                    </labels>
                    <control id=""estimatedvalue"" classid=""{533B9E00-756B-4312-95A0-DC888637AC78}"" datafieldname=""estimatedvalue"" disabled=""false"" />
                  </cell>
                </row>
                <row>
                  <cell id=""estimatedclosedate"">
                    <labels>
                      <label description=""Est. Close Date"" languagecode=""1033"" />
                    </labels>
                    <control id=""estimatedclosedate"" classid=""{5B773807-9FB2-42DB-97C3-7A91EFF8ADFF}"" datafieldname=""estimatedclosedate"" disabled=""false"" />
                  </cell>
                </row>
              </rows>
            </section>
          </sections>
        </column>
      </columns>
    </tab>
  </tabs>
</form>";
        
        var opportunityForm = new Entity("systemform")
        {
            Id = formId,
            ["name"] = "Opportunity Main Form",
            ["objecttypecode"] = "opportunity",
            ["type"] = 2,
            ["formxml"] = formXml,
            ["isdefault"] = true,
            ["iscustomizable"] = true
        };
        service.Create(opportunityForm);
        CreateAppModuleComponent(service, appModuleId, formId, 60);
    }
    
    /// <summary>
    /// Create example web resources (JavaScript files for form scripts)
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource
    /// WebResourceType: 1=HTML, 2=CSS, 3=JavaScript, 4=XML, 5=PNG, 6=JPG, 7=GIF, 8=XAP, 9=XSL, 10=ICO
    /// </summary>
    private static void CreateWebResources(IOrganizationService service)
    {
        Console.WriteLine("  Creating web resources (form scripts)...");
        
        // Create a sample form script for Account
        var accountScriptContent = @"
// Sample Account Form Script
// This script demonstrates the Xrm API usage

function OnLoad(executionContext) {
    console.log('Account form OnLoad event triggered');
    
    var formContext = executionContext.getFormContext();
    
    // Get the account name
    var accountName = formContext.getAttribute('name');
    if (accountName) {
        console.log('Account Name:', accountName.getValue());
        
        // Add onChange handler
        accountName.addOnChange(function() {
            console.log('Account name changed to:', accountName.getValue());
        });
    }
    
    // Example: Disable account number field
    var accountNumberControl = formContext.getControl('accountnumber');
    if (accountNumberControl) {
        accountNumberControl.setDisabled(false);
    }
    
    // Example: Show a notification
    formContext.ui.setFormNotification(
        'Welcome to the Account form!', 
        'INFO', 
        'welcome_notification'
    );
    
    // Clear notification after 3 seconds
    setTimeout(function() {
        formContext.ui.clearFormNotification('welcome_notification');
    }, 3000);
}

function OnSave(executionContext) {
    console.log('Account form OnSave event triggered');
    
    var formContext = executionContext.getFormContext();
    
    // Example: Validate account name is not empty
    var accountName = formContext.getAttribute('name');
    if (accountName && !accountName.getValue()) {
        executionContext.getEventArgs().preventDefault();
        Xrm.Navigation.openAlertDialog({
            text: 'Account Name is required',
            title: 'Validation Error'
        });
        return false;
    }
    
    return true;
}

function ValidateAccountName(executionContext) {
    var formContext = executionContext.getFormContext();
    var accountName = formContext.getAttribute('name');
    
    if (accountName) {
        var value = accountName.getValue();
        if (value && value.length < 3) {
            formContext.ui.setFormNotification(
                'Account name should be at least 3 characters', 
                'WARNING', 
                'name_validation'
            );
        } else {
            formContext.ui.clearFormNotification('name_validation');
        }
    }
}
";
        
        // Encode script content to base64
        var base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(accountScriptContent));
        
        var webResourceId = Guid.NewGuid();
        var webResource = new Entity("webresource")
        {
            Id = webResourceId,
            ["name"] = "fake4dataverse_account_form_script.js",
            ["displayname"] = "Account Form Script",
            ["description"] = "Example form script for Account entity",
            ["webresourcetype"] = 3, // JavaScript
            ["content"] = base64Content,
            ["languagecode"] = 1033
        };
        service.Create(webResource);
        Console.WriteLine($"    Created WebResource: {webResourceId} (fake4dataverse_account_form_script.js)");
        
        // Create a sample utility script
        var utilityScriptContent = @"
// Sample Utility Functions
// Common functions that can be used across forms

var Fake4DataverseUtils = Fake4DataverseUtils || {};

Fake4DataverseUtils.formatPhoneNumber = function(phoneNumber) {
    // Simple phone number formatting
    if (!phoneNumber) return '';
    var cleaned = phoneNumber.replace(/\D/g, '');
    if (cleaned.length === 10) {
        return '(' + cleaned.substring(0, 3) + ') ' + 
               cleaned.substring(3, 6) + '-' + 
               cleaned.substring(6);
    }
    return phoneNumber;
};

Fake4DataverseUtils.validateEmail = function(email) {
    if (!email) return false;
    var re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
};

console.log('Fake4Dataverse utility functions loaded');
";
        
        var utilityBase64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(utilityScriptContent));
        
        var utilityResourceId = Guid.NewGuid();
        var utilityResource = new Entity("webresource")
        {
            Id = utilityResourceId,
            ["name"] = "fake4dataverse_utils.js",
            ["displayname"] = "Fake4Dataverse Utilities",
            ["description"] = "Common utility functions",
            ["webresourcetype"] = 3, // JavaScript
            ["content"] = utilityBase64Content,
            ["languagecode"] = 1033
        };
        service.Create(utilityResource);
        Console.WriteLine($"    Created WebResource: {utilityResourceId} (fake4dataverse_utils.js)");
    }

    /// <summary>
    /// Create the Active solution - a special Microsoft solution that contains all components
    /// Reference: https://learn.microsoft.com/en-us/power-platform/alm/solution-concepts-alm#active-solution
    /// The Active solution uses Microsoft's standard GUID: FD140AAF-4DF4-11DD-BD17-0019B9312238
    /// This solution is not modifiable by users and automatically contains all components
    /// </summary>
    private static void CreateActiveSolution(IOrganizationService service)
    {
        // Use the standard Microsoft Active solution GUID
        // Reference: This is a well-known GUID used across all Dataverse environments
        var activeSolutionId = new Guid("FD140AAF-4DF4-11DD-BD17-0019B9312238");
        
        // Check if Active solution already exists
        try
        {
            var existingSolution = service.Retrieve("solution", activeSolutionId, new Microsoft.Xrm.Sdk.Query.ColumnSet("solutionid"));
            Console.WriteLine($"  Active solution already exists: {activeSolutionId}");
            return;
        }
        catch
        {
            // Solution doesn't exist, create it
        }

        var activeSolution = new Entity("solution")
        {
            Id = activeSolutionId,
            ["uniquename"] = "Active",
            ["friendlyname"] = "Active",
            ["version"] = "1.0.0.0",
            ["ismanaged"] = false,
            ["isvisible"] = true,
            ["description"] = "Special solution containing all components in the system"
        };
        
        service.Create(activeSolution);
        Console.WriteLine($"  Created Active solution: {activeSolutionId}");
    }
}
