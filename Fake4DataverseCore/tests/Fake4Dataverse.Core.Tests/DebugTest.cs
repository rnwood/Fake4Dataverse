using Xunit;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Integrity;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System;
using Fake4Dataverse.Extensions;

namespace DebugTests
{
    public class AttributeMetadataTest
    {
        [Fact]
        public void Debug_AttributeExistsInMetadata()
        {
            var context = XrmFakedContextFactory.New(new IntegrityOptions 
            { 
                ValidateEntityReferences = false,
                ValidateAttributeTypes = false
            });

            var service = context.GetOrganizationService();

            Entity entity = new Entity("entity");
            entity.Id = Guid.NewGuid();
            entity["int"] = 1;
            entity["text"] = "first";

            context.Initialize(new List<Entity> { entity });

            var entityMetadata = context.GetEntityMetadataByName("entity");
            var attExists = context.AttributeExistsInMetadata("entity", "text");
            
            Console.WriteLine($"Entity metadata for 'entity': {(entityMetadata != null ? "EXISTS" : "NULL")}");
            Console.WriteLine($"AttributeExistsInMetadata('entity', 'text'): {attExists}");
            
            if (entityMetadata != null)
            {
                Console.WriteLine($"Entity metadata LogicalName: {entityMetadata.LogicalName}");
                Console.WriteLine($"Entity metadata has Attributes: {(entityMetadata.Attributes != null ? entityMetadata.Attributes.Length.ToString() : "NULL")}");
            }

            // This should not fail when validation is disabled
            QueryExpression query = new QueryExpression("entity");
            query.ColumnSet = new ColumnSet("text");

            EntityCollection result = service.RetrieveMultiple(query);
            Assert.Single(result.Entities);
        }
    }
}
