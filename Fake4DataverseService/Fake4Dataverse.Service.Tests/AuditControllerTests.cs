using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Xunit;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Service.Controllers;
using Microsoft.AspNetCore.Mvc;
using Fake4Dataverse.Abstractions.Audit;

namespace Fake4Dataverse.Service.Tests;

/// <summary>
/// Tests for the AuditController REST API
/// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
/// 
/// These tests verify that the audit REST API correctly exposes audit records
/// and allows querying audit history for entities and specific records.
/// </summary>
public class AuditControllerTests
{
    private readonly AuditController _controller;
    private readonly IOrganizationService _organizationService;
    private readonly IAuditRepository _auditRepository;

    public AuditControllerTests()
    {
        // Create a Fake4Dataverse context with basic configuration
        var context = XrmFakedContextFactory.New();
        _organizationService = context.GetOrganizationService();
        _auditRepository = context.GetProperty<IAuditRepository>();

        // Create the controller
        _controller = new AuditController(context);
    }

    [Fact]
    public void Should_Return_Empty_Audit_List_When_Auditing_Disabled()
    {
        // Arrange
        _auditRepository.IsAuditEnabled = false;

        // Act
        var result = _controller.GetAllAudits(null, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var valueProperty = value.GetType().GetProperty("value");
        var countProperty = value.GetType().GetProperty("count");
        
        Assert.NotNull(valueProperty);
        Assert.NotNull(countProperty);
        
        var auditRecords = valueProperty.GetValue(value) as System.Collections.IEnumerable;
        Assert.NotNull(auditRecords);
        Assert.Empty(auditRecords.Cast<object>());
        
        Assert.Equal(0, countProperty.GetValue(value));
    }

    [Fact]
    public void Should_Return_Audit_Records_When_Auditing_Enabled()
    {
        // Arrange - Enable auditing
        _auditRepository.IsAuditEnabled = true;

        // Create an entity to generate audit records
        var account = new Entity("account")
        {
            ["name"] = "Test Account"
        };
        var accountId = _organizationService.Create(account);

        // Act
        var result = _controller.GetAllAudits(null, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var valueProperty = value.GetType().GetProperty("value");
        var countProperty = value.GetType().GetProperty("count");
        
        Assert.NotNull(valueProperty);
        Assert.NotNull(countProperty);
        
        var auditRecords = valueProperty.GetValue(value) as System.Collections.IEnumerable;
        Assert.NotNull(auditRecords);
        Assert.NotEmpty(auditRecords.Cast<object>());
        
        var count = (int)countProperty.GetValue(value);
        Assert.True(count > 0);
    }

    [Fact]
    public void Should_Return_Entity_Audit_History()
    {
        // Arrange - Enable auditing
        _auditRepository.IsAuditEnabled = true;

        // Create and update an entity to generate audit records
        var account = new Entity("account")
        {
            ["name"] = "Test Account"
        };
        var accountId = _organizationService.Create(account);

        // Update the account
        var updateAccount = new Entity("account", accountId)
        {
            ["name"] = "Updated Account"
        };
        _organizationService.Update(updateAccount);

        // Act
        var result = _controller.GetEntityAudits("account", accountId.ToString());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var valueProperty = value.GetType().GetProperty("value");
        var countProperty = value.GetType().GetProperty("count");
        
        Assert.NotNull(valueProperty);
        Assert.NotNull(countProperty);
        
        var auditRecords = valueProperty.GetValue(value) as System.Collections.IEnumerable;
        Assert.NotNull(auditRecords);
        
        var recordsList = auditRecords.Cast<object>().ToList();
        Assert.True(recordsList.Count >= 2); // At least Create and Update
        
        var count = (int)countProperty.GetValue(value);
        Assert.True(count >= 2);
    }

    [Fact]
    public void Should_Return_Audit_Details()
    {
        // Arrange - Enable auditing
        _auditRepository.IsAuditEnabled = true;

        // Create an entity to generate audit record
        var account = new Entity("account")
        {
            ["name"] = "Test Account",
            ["revenue"] = new Money(100000)
        };
        var accountId = _organizationService.Create(account);

        // Get the audit records
        var entityRef = new EntityReference("account", accountId);
        var auditRecords = _auditRepository.GetAuditRecordsForEntity(entityRef).ToList();
        Assert.NotEmpty(auditRecords);

        var auditId = auditRecords.First().GetAttributeValue<Guid>("auditid");

        // Act
        var result = _controller.GetAuditDetails(auditId.ToString());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void Should_Return_Audit_Status()
    {
        // Arrange
        _auditRepository.IsAuditEnabled = true;

        // Act
        var result = _controller.GetAuditStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var isAuditEnabledProperty = value.GetType().GetProperty("isAuditEnabled");
        
        Assert.NotNull(isAuditEnabledProperty);
        var isEnabled = (bool)isAuditEnabledProperty.GetValue(value);
        Assert.True(isEnabled);
    }

    [Fact]
    public void Should_Set_Audit_Status()
    {
        // Arrange
        _auditRepository.IsAuditEnabled = false;
        var request = new AuditStatusRequest { IsAuditEnabled = true };

        // Act
        var result = _controller.SetAuditStatus(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var isAuditEnabledProperty = value.GetType().GetProperty("isAuditEnabled");
        
        Assert.NotNull(isAuditEnabledProperty);
        var isEnabled = (bool)isAuditEnabledProperty.GetValue(value);
        Assert.True(isEnabled);
        Assert.True(_auditRepository.IsAuditEnabled);
    }

    [Fact]
    public void Should_Return_BadRequest_For_Invalid_Guid()
    {
        // Act
        var result = _controller.GetEntityAudits("account", "invalid-guid");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Should_Support_Filtering_By_Entity_Type()
    {
        // Arrange - Enable auditing
        _auditRepository.IsAuditEnabled = true;

        // Create different entity types
        var account = new Entity("account") { ["name"] = "Test Account" };
        var contact = new Entity("contact") { ["firstname"] = "Test" };
        
        _organizationService.Create(account);
        _organizationService.Create(contact);

        // Act - Filter by account entity type
        var result = _controller.GetAllAudits(null, null, null, "objecttypecode eq 'account'");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var valueProperty = value.GetType().GetProperty("value");
        
        Assert.NotNull(valueProperty);
        var auditRecords = valueProperty.GetValue(value) as System.Collections.IEnumerable;
        Assert.NotNull(auditRecords);
        
        // All records should be for account entity
        var recordsList = auditRecords.Cast<object>().ToList();
        Assert.NotEmpty(recordsList);
    }

    [Fact]
    public void Should_Support_Pagination()
    {
        // Arrange - Enable auditing
        _auditRepository.IsAuditEnabled = true;

        // Create multiple entities to generate audit records
        for (int i = 0; i < 5; i++)
        {
            var account = new Entity("account") { ["name"] = $"Test Account {i}" };
            _organizationService.Create(account);
        }

        // Act - Request first 2 records
        var result = _controller.GetAllAudits(2, 0, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var valueProperty = value.GetType().GetProperty("value");
        var countProperty = value.GetType().GetProperty("count");
        
        Assert.NotNull(valueProperty);
        Assert.NotNull(countProperty);
        
        var auditRecords = valueProperty.GetValue(value) as System.Collections.IEnumerable;
        Assert.NotNull(auditRecords);
        
        var recordsList = auditRecords.Cast<object>().ToList();
        Assert.Equal(2, recordsList.Count);
        
        // Total count should be more than 2
        var totalCount = (int)countProperty.GetValue(value);
        Assert.True(totalCount >= 5);
    }
}
