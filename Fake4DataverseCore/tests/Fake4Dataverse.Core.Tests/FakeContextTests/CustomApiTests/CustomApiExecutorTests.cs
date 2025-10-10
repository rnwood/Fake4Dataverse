using Fake4Dataverse.Abstractions;
using Fake4Dataverse.FakeMessageExecutors;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.CustomApiTests
{
    /// <summary>
    /// Tests for Custom API execution support.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
    /// 
    /// Custom APIs are the modern way to create custom messages in Dataverse.
    /// They provide:
    /// - Strongly-typed request/response parameters
    /// - Support for both Functions (read) and Actions (write)
    /// - Entity-bound or global scope
    /// - Integration with the plugin pipeline
    /// </summary>
    public class CustomApiExecutorTests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public CustomApiExecutorTests()
        {
            _context = XrmFakedContextFactory.New();
            _service = _context.GetOrganizationService();
        }

        [Fact]
        public void Should_Execute_Simple_Custom_Api_With_No_Parameters()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
            // A Custom API without parameters is the simplest form
            
            // Arrange - Create a Custom API definition
            var customApi = new Entity("customapi")
            {
                Id = Guid.NewGuid(),
                ["uniquename"] = "sample_SimpleCustomApi",
                ["displayname"] = "Simple Custom API",
                ["bindingtype"] = new OptionSetValue(0), // 0 = Global
                ["boundentitylogicalname"] = null,
                ["isfunction"] = false, // Action
                ["isenabled"] = true,
                ["executeprivilegename"] = null
            };

            _context.Initialize(new[] { customApi });

            // Act - Execute the Custom API
            var request = new OrganizationRequest("sample_SimpleCustomApi");
            var response = _service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("sample_SimpleCustomApi", response.ResponseName);
        }

        [Fact]
        public void Should_Execute_Custom_Api_With_Input_And_Output_Parameters()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables
            // Custom APIs can have both request and response parameters
            
            // Arrange - Create a Custom API definition with parameters
            var customApiId = Guid.NewGuid();
            var customApi = new Entity("customapi")
            {
                Id = customApiId,
                ["uniquename"] = "sample_CalculateTotal",
                ["displayname"] = "Calculate Total",
                ["bindingtype"] = new OptionSetValue(0), // Global
                ["isfunction"] = true, // Function (read operation)
                ["isenabled"] = true
            };

            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#customapirequestparameter-table-columns
            // Input parameter: Amount (Decimal, Required)
            var inputParam = new Entity("customapirequestparameter")
            {
                Id = Guid.NewGuid(),
                ["customapiid"] = new EntityReference("customapi", customApiId),
                ["uniquename"] = "Amount",
                ["displayname"] = "Amount",
                ["type"] = new OptionSetValue(2), // 2 = Decimal
                ["isoptional"] = false
            };

            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#customapiresponseparameter-table-columns
            // Output parameter: Total (Decimal)
            var outputParam = new Entity("customapiresponseparameter")
            {
                Id = Guid.NewGuid(),
                ["customapiid"] = new EntityReference("customapi", customApiId),
                ["uniquename"] = "Total",
                ["displayname"] = "Total",
                ["type"] = new OptionSetValue(2) // 2 = Decimal
            };

            _context.Initialize(new Entity[] { customApi, inputParam, outputParam });

            // Act - Execute the Custom API with input parameter
            var request = new OrganizationRequest("sample_CalculateTotal");
            request.Parameters["Amount"] = 100.50m;
            var response = _service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("sample_CalculateTotal", response.ResponseName);
            Assert.True(response.Results.Contains("Total"));
            Assert.IsType<decimal>(response.Results["Total"]);
        }

        [Fact]
        public void Should_Throw_Exception_When_Custom_Api_Not_Found()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
            // If a Custom API is not registered, execution should fail
            
            // Act & Assert
            var request = new OrganizationRequest("sample_NonExistentApi");
            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Execute(request));
            Assert.Contains("not registered", exception.Message);
        }

        [Fact]
        public void Should_Throw_Exception_When_Custom_Api_Is_Disabled()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#customapi-table-columns
            // The isenabled column controls whether the API can be executed
            
            // Arrange - Create a disabled Custom API
            var customApi = new Entity("customapi")
            {
                Id = Guid.NewGuid(),
                ["uniquename"] = "sample_DisabledApi",
                ["displayname"] = "Disabled API",
                ["bindingtype"] = new OptionSetValue(0),
                ["isfunction"] = false,
                ["isenabled"] = false // Disabled
            };

            _context.Initialize(new[] { customApi });

            // Act & Assert
            var request = new OrganizationRequest("sample_DisabledApi");
            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Execute(request));
            Assert.Contains("not enabled", exception.Message);
        }

        [Fact]
        public void Should_Throw_Exception_When_Required_Parameter_Missing()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#customapirequestparameter-table-columns
            // Required parameters (isoptional = false) must be provided
            
            // Arrange - Create Custom API with required parameter
            var customApiId = Guid.NewGuid();
            var customApi = new Entity("customapi")
            {
                Id = customApiId,
                ["uniquename"] = "sample_RequiredParamApi",
                ["displayname"] = "Required Param API",
                ["bindingtype"] = new OptionSetValue(0),
                ["isfunction"] = false,
                ["isenabled"] = true
            };

            var requiredParam = new Entity("customapirequestparameter")
            {
                Id = Guid.NewGuid(),
                ["customapiid"] = new EntityReference("customapi", customApiId),
                ["uniquename"] = "RequiredInput",
                ["displayname"] = "Required Input",
                ["type"] = new OptionSetValue(10), // String
                ["isoptional"] = false
            };

            _context.Initialize(new Entity[] { customApi, requiredParam });

            // Act & Assert - Execute without required parameter
            var request = new OrganizationRequest("sample_RequiredParamApi");
            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Execute(request));
            Assert.Contains("Required parameter", exception.Message);
        }

        [Fact]
        public void Should_Execute_Custom_Api_With_Optional_Parameters()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#customapirequestparameter-table-columns
            // Optional parameters (isoptional = true) can be omitted
            
            // Arrange - Create Custom API with optional parameter
            var customApiId = Guid.NewGuid();
            var customApi = new Entity("customapi")
            {
                Id = customApiId,
                ["uniquename"] = "sample_OptionalParamApi",
                ["displayname"] = "Optional Param API",
                ["bindingtype"] = new OptionSetValue(0),
                ["isfunction"] = false,
                ["isenabled"] = true
            };

            var optionalParam = new Entity("customapirequestparameter")
            {
                Id = Guid.NewGuid(),
                ["customapiid"] = new EntityReference("customapi", customApiId),
                ["uniquename"] = "OptionalInput",
                ["displayname"] = "Optional Input",
                ["type"] = new OptionSetValue(10), // String
                ["isoptional"] = true
            };

            _context.Initialize(new Entity[] { customApi, optionalParam });

            // Act - Execute without optional parameter
            var request = new OrganizationRequest("sample_OptionalParamApi");
            var response = _service.Execute(request);

            // Assert - Should succeed
            Assert.NotNull(response);
            Assert.Equal("sample_OptionalParamApi", response.ResponseName);
        }

        [Fact]
        public void Should_Handle_Entity_Bound_Custom_Api()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
            // Custom APIs can be entity-bound (bindingtype = 1) requiring a Target parameter
            
            // Arrange - Create entity-bound Custom API
            var customApiId = Guid.NewGuid();
            var customApi = new Entity("customapi")
            {
                Id = customApiId,
                ["uniquename"] = "sample_EntityBoundApi",
                ["displayname"] = "Entity Bound API",
                ["bindingtype"] = new OptionSetValue(1), // 1 = Entity
                ["boundentitylogicalname"] = "account",
                ["isfunction"] = false,
                ["isenabled"] = true
            };

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            _context.Initialize(new Entity[] { customApi, account });

            // Act - Execute entity-bound Custom API with Target parameter
            var request = new OrganizationRequest("sample_EntityBoundApi");
            request.Parameters["Target"] = new EntityReference("account", account.Id);
            var response = _service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("sample_EntityBoundApi", response.ResponseName);
        }

        [Fact]
        public void Should_Handle_Multiple_Output_Parameters_With_Different_Types()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#parameter-data-types
            // Custom APIs support various data types for parameters
            
            // Arrange - Create Custom API with multiple output parameters
            var customApiId = Guid.NewGuid();
            var customApi = new Entity("customapi")
            {
                Id = customApiId,
                ["uniquename"] = "sample_MultiOutputApi",
                ["displayname"] = "Multi Output API",
                ["bindingtype"] = new OptionSetValue(0),
                ["isfunction"] = true,
                ["isenabled"] = true
            };

            // Different output parameter types
            var stringOutput = new Entity("customapiresponseparameter")
            {
                Id = Guid.NewGuid(),
                ["customapiid"] = new EntityReference("customapi", customApiId),
                ["uniquename"] = "StringOutput",
                ["type"] = new OptionSetValue(10) // String
            };

            var integerOutput = new Entity("customapiresponseparameter")
            {
                Id = Guid.NewGuid(),
                ["customapiid"] = new EntityReference("customapi", customApiId),
                ["uniquename"] = "IntegerOutput",
                ["type"] = new OptionSetValue(7) // Integer
            };

            var booleanOutput = new Entity("customapiresponseparameter")
            {
                Id = Guid.NewGuid(),
                ["customapiid"] = new EntityReference("customapi", customApiId),
                ["uniquename"] = "BooleanOutput",
                ["type"] = new OptionSetValue(0) // Boolean
            };

            _context.Initialize(new Entity[] { customApi, stringOutput, integerOutput, booleanOutput });

            // Act
            var request = new OrganizationRequest("sample_MultiOutputApi");
            var response = _service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Results.Contains("StringOutput"));
            Assert.True(response.Results.Contains("IntegerOutput"));
            Assert.True(response.Results.Contains("BooleanOutput"));
            Assert.IsType<string>(response.Results["StringOutput"]);
            Assert.IsType<int>(response.Results["IntegerOutput"]);
            Assert.IsType<bool>(response.Results["BooleanOutput"]);
        }

        [Fact]
        public void Should_Not_Execute_Standard_Sdk_Messages_As_Custom_Api()
        {
            // Custom API executor should not intercept standard SDK messages
            
            // Arrange
            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };
            _context.Initialize(new[] { account });

            // Act - Execute standard Create request
            var createRequest = new OrganizationRequest("Create");
            createRequest.Parameters["Target"] = new Entity("account") { ["name"] = "New Account" };
            
            // This should be handled by CreateRequestExecutor, not CustomApiExecutor
            var executor = new CustomApiExecutor();
            var canExecute = executor.CanExecute(createRequest);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void Should_Handle_Null_Request()
        {
            // Test error handling for null request
            var executor = new CustomApiExecutor();
            
            Assert.False(executor.CanExecute(null));
        }

        [Fact]
        public void Should_Handle_Request_With_Empty_RequestName()
        {
            // Test error handling for empty RequestName
            var request = new OrganizationRequest();
            var executor = new CustomApiExecutor();
            
            Assert.False(executor.CanExecute(request));
        }
    }
}
