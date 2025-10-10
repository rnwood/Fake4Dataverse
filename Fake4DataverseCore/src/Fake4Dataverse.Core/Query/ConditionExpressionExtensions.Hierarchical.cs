using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Fake4Dataverse.Abstractions;

namespace Fake4Dataverse.Query
{
    public static partial class ConditionExpressionExtensions
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
        // Hierarchical operators are used to query hierarchical data structures where entities have self-referencing relationships
        // For example, Account entities can have a parentaccountid that references another Account
        
        internal static Expression ToHierarchicalExpression(this TypedConditionExpression tc, 
            QueryExpression qe,
            IXrmFakedContext context, 
            Expression getAttributeValueExpr, 
            Expression containsAttributeExpr,
            ParameterExpression entity)
        {
            var c = tc.CondExpression;
            
            // Get the value being compared (the record ID we're comparing against in the hierarchy)
            var compareValue = c.Values.Count > 0 ? c.Values[0] : null;
            
            if (compareValue == null)
            {
                throw new Exception($"Hierarchical operator {c.Operator} requires a value to compare against.");
            }

            // Convert to Guid if it's an EntityReference
            Guid compareGuid;
            if (compareValue is EntityReference entityRef)
            {
                compareGuid = entityRef.Id;
            }
            else if (compareValue is Guid guid)
            {
                compareGuid = guid;
            }
            else
            {
                throw new Exception($"Hierarchical operator {c.Operator} requires a Guid or EntityReference value.");
            }

            // Get all entities of this type from the context
            var entityLogicalName = qe.EntityName;
            var allEntities = context.CreateQuery(entityLogicalName).ToList();
            
            // Build hierarchy map: child ID -> parent ID
            var hierarchyMap = new Dictionary<Guid, Guid>();
            foreach (var e in allEntities)
            {
                if (e.Contains(c.AttributeName) && e[c.AttributeName] != null)
                {
                    var parentRef = e[c.AttributeName] as EntityReference;
                    if (parentRef != null)
                    {
                        hierarchyMap[e.Id] = parentRef.Id;
                    }
                }
            }

            // Get the set of IDs that match the hierarchical condition
            HashSet<Guid> matchingIds = new HashSet<Guid>();

            switch (c.Operator)
            {
                case ConditionOperator.Under:
                    // Returns all descendants (not including the record itself)
                    matchingIds = GetDescendants(compareGuid, hierarchyMap, includeRoot: false);
                    break;

                case ConditionOperator.UnderOrEqual:
                    // Returns all descendants including the record itself
                    matchingIds = GetDescendants(compareGuid, hierarchyMap, includeRoot: true);
                    break;

                case ConditionOperator.Above:
                    // Returns all ancestors (not including the record itself)
                    matchingIds = GetAncestors(compareGuid, hierarchyMap, includeRoot: false);
                    break;

                case ConditionOperator.AboveOrEqual:
                    // Returns all ancestors including the record itself
                    matchingIds = GetAncestors(compareGuid, hierarchyMap, includeRoot: true);
                    break;

                case ConditionOperator.NotUnder:
                    // Returns all records that are NOT descendants of the specified record
                    var underIds = GetDescendants(compareGuid, hierarchyMap, includeRoot: true);
                    matchingIds = new HashSet<Guid>(allEntities.Select(e => e.Id).Except(underIds));
                    break;

                default:
                    throw new Exception($"Unsupported hierarchical operator: {c.Operator}");
            }

            // Create expression that checks if the entity's ID is in the matching set
            var idProperty = Expression.Property(entity, "Id");
            var matchingIdsConstant = Expression.Constant(matchingIds);
            var containsMethod = typeof(HashSet<Guid>).GetMethod("Contains", new[] { typeof(Guid) });
            var containsExpression = Expression.Call(matchingIdsConstant, containsMethod, idProperty);

            return containsExpression;
        }

        private static HashSet<Guid> GetDescendants(Guid rootId, Dictionary<Guid, Guid> hierarchyMap, bool includeRoot)
        {
            var result = new HashSet<Guid>();
            
            if (includeRoot)
            {
                result.Add(rootId);
            }

            // Find all children recursively
            var toProcess = new Queue<Guid>();
            toProcess.Enqueue(rootId);

            while (toProcess.Count > 0)
            {
                var currentId = toProcess.Dequeue();
                
                // Find all entities where parent is currentId
                var children = hierarchyMap.Where(kvp => kvp.Value == currentId).Select(kvp => kvp.Key);
                
                foreach (var childId in children)
                {
                    if (!result.Contains(childId))
                    {
                        result.Add(childId);
                        toProcess.Enqueue(childId);
                    }
                }
            }

            return result;
        }

        private static HashSet<Guid> GetAncestors(Guid childId, Dictionary<Guid, Guid> hierarchyMap, bool includeRoot)
        {
            var result = new HashSet<Guid>();
            
            if (includeRoot)
            {
                result.Add(childId);
            }

            var currentId = childId;
            
            // Walk up the hierarchy
            while (hierarchyMap.ContainsKey(currentId))
            {
                var parentId = hierarchyMap[currentId];
                if (!result.Contains(parentId))
                {
                    result.Add(parentId);
                    currentId = parentId;
                }
                else
                {
                    // Circular reference detected, stop
                    break;
                }
            }

            return result;
        }
    }
}
