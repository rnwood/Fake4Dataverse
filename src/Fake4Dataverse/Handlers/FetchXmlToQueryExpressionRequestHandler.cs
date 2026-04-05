using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles the FetchXmlToQueryExpression request by converting a FetchXml string to a <see cref="QueryExpression"/>.
    /// </summary>
    internal sealed class FetchXmlToQueryExpressionRequestHandler : IOrganizationRequestHandler
    {
        /// <inheritdoc />
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "FetchXmlToQueryExpression", StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc />
        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var fetchXml = (string)request["FetchXml"];
            if (string.IsNullOrEmpty(fetchXml))
                throw new ArgumentException("FetchXml is required.");

            var doc = XDocument.Parse(fetchXml);
            var entityElement = doc.Root?.Element("entity");
            if (entityElement == null)
                throw new InvalidOperationException("FetchXml must contain an entity element.");

            var entityName = entityElement.Attribute("name")?.Value
                ?? throw new InvalidOperationException("Entity name is required.");

            var query = new QueryExpression(entityName);

            // Parse columns
            foreach (var attr in entityElement.Elements("attribute"))
            {
                var name = attr.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                    query.ColumnSet.AddColumn(name);
            }

            if (entityElement.Element("all-attributes") != null)
                query.ColumnSet.AllColumns = true;
            else if (query.ColumnSet.Columns.Count == 0)
                query.ColumnSet.AllColumns = true;

            // Parse filter
            var filterElement = entityElement.Element("filter");
            if (filterElement != null)
                ParseFilter(filterElement, query.Criteria);

            // Parse order
            foreach (var order in entityElement.Elements("order"))
            {
                var attrName = order.Attribute("attribute")?.Value;
                var descending = string.Equals(order.Attribute("descending")?.Value, "true", StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(attrName))
                    query.AddOrder(attrName, descending ? OrderType.Descending : OrderType.Ascending);
            }

            // Parse top/count
            var topAttr = doc.Root?.Attribute("top");
            if (topAttr != null && int.TryParse(topAttr.Value, out var topVal))
                query.TopCount = topVal;

            var countAttr = doc.Root?.Attribute("count");
            if (countAttr != null && int.TryParse(countAttr.Value, out var count))
                query.TopCount = count;

            var distinctAttr = doc.Root?.Attribute("distinct");
            if (distinctAttr != null && string.Equals(distinctAttr.Value, "true", StringComparison.OrdinalIgnoreCase))
                query.Distinct = true;

            // Parse link-entities
            foreach (var linkEl in entityElement.Elements("link-entity"))
                ParseLinkEntity(linkEl, query.LinkEntities, entityName);

            var response = new OrganizationResponse { ResponseName = "FetchXmlToQueryExpression" };
            response["Query"] = query;
            return response;
        }

        private static void ParseFilter(XElement filterElement, FilterExpression filter)
        {
            var type = filterElement.Attribute("type")?.Value;
            filter.FilterOperator = string.Equals(type, "or", StringComparison.OrdinalIgnoreCase)
                ? LogicalOperator.Or
                : LogicalOperator.And;

            foreach (var condition in filterElement.Elements("condition"))
            {
                var attribute = condition.Attribute("attribute")?.Value;
                var operatorStr = condition.Attribute("operator")?.Value;
                var value = condition.Attribute("value")?.Value;

                if (string.IsNullOrEmpty(attribute) || string.IsNullOrEmpty(operatorStr))
                    continue;

                var op = ParseOperator(operatorStr!);

                if (value != null)
                {
                    filter.AddCondition(attribute, op, value);
                }
                else
                {
                    // Check for child <value> elements (for In, Between, etc.)
                    var childValues = new List<object>();
                    foreach (var v in condition.Elements("value"))
                        childValues.Add(v.Value);

                    if (childValues.Count > 0)
                        filter.AddCondition(attribute, op, childValues.ToArray());
                    else
                        filter.AddCondition(attribute, op);
                }
            }

            foreach (var subFilter in filterElement.Elements("filter"))
            {
                var child = new FilterExpression();
                ParseFilter(subFilter, child);
                filter.AddFilter(child);
            }
        }

        private static void ParseLinkEntity(XElement linkEl, DataCollection<LinkEntity> linkEntities, string parentEntityName)
        {
            var linkToEntity = linkEl.Attribute("name")?.Value ?? string.Empty;
            var fromAttr = linkEl.Attribute("from")?.Value ?? string.Empty;
            var toAttr = linkEl.Attribute("to")?.Value ?? string.Empty;
            var linkTypeStr = linkEl.Attribute("link-type")?.Value;
            var alias = linkEl.Attribute("alias")?.Value;

            var joinOp = string.Equals(linkTypeStr, "outer", StringComparison.OrdinalIgnoreCase)
                ? JoinOperator.LeftOuter
                : JoinOperator.Inner;

            var link = new LinkEntity
            {
                LinkFromEntityName = parentEntityName,
                LinkFromAttributeName = toAttr,
                LinkToEntityName = linkToEntity,
                LinkToAttributeName = fromAttr,
                JoinOperator = joinOp
            };

            if (!string.IsNullOrEmpty(alias))
                link.EntityAlias = alias;

            // Parse columns
            foreach (var attr in linkEl.Elements("attribute"))
            {
                var name = attr.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                    link.Columns.AddColumn(name);
            }

            if (linkEl.Element("all-attributes") != null)
                link.Columns.AllColumns = true;

            // Parse filter
            var filterElement = linkEl.Element("filter");
            if (filterElement != null)
                ParseFilter(filterElement, link.LinkCriteria);

            // Nested link entities
            foreach (var nestedLink in linkEl.Elements("link-entity"))
                ParseLinkEntity(nestedLink, link.LinkEntities, linkToEntity);

            linkEntities.Add(link);
        }

        private static ConditionOperator ParseOperator(string op)
        {
            switch (op.ToLowerInvariant())
            {
                case "eq": return ConditionOperator.Equal;
                case "ne": case "neq": return ConditionOperator.NotEqual;
                case "gt": return ConditionOperator.GreaterThan;
                case "ge": case "gte": return ConditionOperator.GreaterEqual;
                case "lt": return ConditionOperator.LessThan;
                case "le": case "lte": return ConditionOperator.LessEqual;
                case "like": return ConditionOperator.Like;
                case "not-like": return ConditionOperator.NotLike;
                case "null": return ConditionOperator.Null;
                case "not-null": return ConditionOperator.NotNull;
                case "in": return ConditionOperator.In;
                case "not-in": return ConditionOperator.NotIn;
                case "begins-with": return ConditionOperator.BeginsWith;
                case "not-begin-with": return ConditionOperator.DoesNotBeginWith;
                case "ends-with": return ConditionOperator.EndsWith;
                case "not-end-with": return ConditionOperator.DoesNotEndWith;
                case "contains": return ConditionOperator.Contains;
                case "not-contain": return ConditionOperator.DoesNotContain;
                default: return ConditionOperator.Equal;
            }
        }
    }
}
