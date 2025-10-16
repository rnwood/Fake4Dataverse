using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fake4Dataverse.Tests
{
    /// <summary>
    /// Tests for thread safety in CRUD operations
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/overview
    /// 
    /// The Fake4Dataverse framework now provides database-like serialization of transactions
    /// to ensure thread-safe CRUD operations when multiple threads access the same context.
    /// </summary>
    public class ThreadSafetyTests : Fake4DataverseTests
    {
        [Fact]
        public void Should_Handle_Concurrent_Creates_Without_Race_Conditions()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;
            
            var createCount = 100;
            var createdIds = new List<Guid>();
            var lockObject = new object();
            
            // Act - Create multiple accounts concurrently
            Parallel.For(0, createCount, i =>
            {
                var account = new Entity("account")
                {
                    ["name"] = $"Account {i}"
                };
                
                var id = service.Create(account);
                
                lock (lockObject)
                {
                    createdIds.Add(id);
                }
            });
            
            // Assert - All accounts should be created successfully
            Assert.Equal(createCount, createdIds.Count);
            Assert.Equal(createCount, createdIds.Distinct().Count()); // All IDs should be unique
            
            // Verify all accounts exist in the context
            foreach (var id in createdIds)
            {
                var account = service.Retrieve("account", id, new Microsoft.Xrm.Sdk.Query.ColumnSet("name"));
                Assert.NotNull(account);
            }
        }
        
        [Fact]
        public void Should_Handle_Concurrent_Updates_Without_Data_Loss()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;
            
            // Create initial accounts
            var accountIds = new List<Guid>();
            for (int i = 0; i < 10; i++)
            {
                var id = service.Create(new Entity("account")
                {
                    ["name"] = $"Account {i}",
                    ["revenue"] = new Microsoft.Xrm.Sdk.Money(0)
                });
                accountIds.Add(id);
            }
            
            // Act - Update each account multiple times concurrently
            var updateCount = 10;
            Parallel.ForEach(accountIds, accountId =>
            {
                for (int i = 0; i < updateCount; i++)
                {
                    service.Update(new Entity("account", accountId)
                    {
                        ["revenue"] = new Microsoft.Xrm.Sdk.Money(i + 1)
                    });
                }
            });
            
            // Assert - All accounts should still exist
            foreach (var accountId in accountIds)
            {
                var account = service.Retrieve("account", accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet("revenue"));
                Assert.NotNull(account);
                Assert.True(account.Contains("revenue"));
            }
        }
        
        [Fact]
        public void Should_Handle_Concurrent_Reads_Without_Exceptions()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;
            
            // Create test accounts
            var accountIds = new List<Guid>();
            for (int i = 0; i < 50; i++)
            {
                var id = service.Create(new Entity("account")
                {
                    ["name"] = $"Account {i}"
                });
                accountIds.Add(id);
            }
            
            var exceptions = new List<Exception>();
            var lockObject = new object();
            
            // Act - Read accounts concurrently
            Parallel.ForEach(accountIds, accountId =>
            {
                try
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var account = service.Retrieve("account", accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet("name"));
                        Assert.NotNull(account);
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            
            // Assert - No exceptions should occur
            Assert.Empty(exceptions);
        }
        
        [Fact]
        public void Should_Handle_Concurrent_Deletes_Without_Duplicate_Deletion_Errors()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;
            
            // Create test accounts
            var accountIds = new List<Guid>();
            for (int i = 0; i < 50; i++)
            {
                var id = service.Create(new Entity("account")
                {
                    ["name"] = $"Account {i}"
                });
                accountIds.Add(id);
            }
            
            // Act - Delete accounts concurrently
            Parallel.ForEach(accountIds, accountId =>
            {
                service.Delete("account", accountId);
            });
            
            // Assert - All accounts should be deleted
            foreach (var accountId in accountIds)
            {
                Assert.False(context.ContainsEntity("account", accountId));
            }
        }
        
        [Fact]
        public void Should_Handle_Mixed_Concurrent_CRUD_Operations()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;
            
            var operationCount = 200;
            var exceptions = new List<Exception>();
            var lockObject = new object();
            var random = new Random();
            
            // Act - Perform mixed CRUD operations concurrently
            Parallel.For(0, operationCount, i =>
            {
                try
                {
                    var operation = i % 4;
                    
                    switch (operation)
                    {
                        case 0: // Create
                            service.Create(new Entity("account")
                            {
                                ["name"] = $"Account {i}"
                            });
                            break;
                            
                        case 1: // Read
                            var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("account")
                            {
                                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("name")
                            };
                            service.RetrieveMultiple(query);
                            break;
                            
                        case 2: // Update (if entities exist)
                            var existingQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("account")
                            {
                                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("accountid"),
                                TopCount = 1
                            };
                            var existingResults = service.RetrieveMultiple(existingQuery);
                            if (existingResults.Entities.Count > 0)
                            {
                                var entityToUpdate = existingResults.Entities[0];
                                service.Update(new Entity("account", entityToUpdate.Id)
                                {
                                    ["name"] = $"Updated Account {i}"
                                });
                            }
                            break;
                            
                        case 3: // Delete (if entities exist)
                            var deleteQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("account")
                            {
                                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("accountid"),
                                TopCount = 1
                            };
                            var deleteResults = service.RetrieveMultiple(deleteQuery);
                            if (deleteResults.Entities.Count > 0)
                            {
                                var entityToDelete = deleteResults.Entities[0];
                                service.Delete("account", entityToDelete.Id);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            
            // Assert - No unexpected exceptions should occur
            // Note: Some expected exceptions may occur (e.g., trying to delete already deleted entity)
            // but no race condition exceptions should occur
            Assert.True(exceptions.Count == 0 || exceptions.All(ex => 
                ex.Message.Contains("Does Not Exist") || 
                ex is InvalidOperationException), 
                $"Unexpected exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        }
    }
}
