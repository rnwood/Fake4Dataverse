using System;
using System.Xml.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="QueryExpressionToFetchXmlRequest"/> by converting a
    /// <see cref="QueryExpression"/> to a FetchXML string.
    /// </summary>
    internal sealed class QueryExpressionToFetchXmlRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "QueryExpressionToFetchXml", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var query = (QueryExpression)request["Query"];
            if (query == null)
                throw new ArgumentException("Query parameter is required.");

            var fetchXml = ConvertToFetchXml(query);

            var response = new QueryExpressionToFetchXmlResponse();
            response.Results["FetchXml"] = fetchXml;
            return response;
        }

        private static string ConvertToFetchXml(QueryExpression query)
        {
            var fetchEl = new XElement("fetch",
                new XAttribute("mapping", "logical"));

            if (query.TopCount.HasValue)
                fetchEl.Add(new XAttribute("top", query.TopCount.Value));

            if (query.Distinct)
                fetchEl.Add(new XAttribute("distinct", "true"));

            var entityEl = new XElement("entity",
                new XAttribute("name", query.EntityName));

            // Columns
            if (query.ColumnSet.AllColumns)
            {
                entityEl.Add(new XElement("all-attributes"));
            }
            else
            {
                foreach (var col in query.ColumnSet.Columns)
                    entityEl.Add(new XElement("attribute", new XAttribute("name", col)));
            }

            // Orders
            foreach (var order in query.Orders)
            {
                entityEl.Add(new XElement("order",
                    new XAttribute("attribute", order.AttributeName),
                    new XAttribute("descending", order.OrderType == OrderType.Descending ? "true" : "false")));
            }

            // Filter
            if (query.Criteria != null &&
                (query.Criteria.Conditions.Count > 0 || query.Criteria.Filters.Count > 0))
            {
                entityEl.Add(ConvertFilter(query.Criteria));
            }

            // Link entities
            foreach (var link in query.LinkEntities)
                entityEl.Add(ConvertLinkEntity(link));

            fetchEl.Add(entityEl);
            return fetchEl.ToString();
        }

        private static XElement ConvertFilter(FilterExpression filter)
        {
            var el = new XElement("filter",
                new XAttribute("type", filter.FilterOperator == LogicalOperator.And ? "and" : "or"));

            foreach (var cond in filter.Conditions)
            {
                var condEl = new XElement("condition",
                    new XAttribute("attribute", cond.AttributeName),
                    new XAttribute("operator", GetOperatorString(cond.Operator)));

                if (cond.Values != null && cond.Values.Count > 0)
                {
                    if (cond.Values.Count == 1)
                    {
                        condEl.Add(new XAttribute("value", cond.Values[0]?.ToString() ?? ""));
                    }
                    else
                    {
                        foreach (var val in cond.Values)
                            condEl.Add(new XElement("value", val?.ToString() ?? ""));
                    }
                }

                el.Add(condEl);
            }

            foreach (var child in filter.Filters)
                el.Add(ConvertFilter(child));

            return el;
        }

        private static XElement ConvertLinkEntity(LinkEntity link)
        {
            var el = new XElement("link-entity",
                new XAttribute("name", link.LinkToEntityName),
                new XAttribute("from", link.LinkToAttributeName),
                new XAttribute("to", link.LinkFromAttributeName));

            if (link.JoinOperator == JoinOperator.LeftOuter)
                el.Add(new XAttribute("link-type", "outer"));
            else
                el.Add(new XAttribute("link-type", "inner"));

            if (!string.IsNullOrEmpty(link.EntityAlias))
                el.Add(new XAttribute("alias", link.EntityAlias));

            if (link.Columns.AllColumns)
            {
                el.Add(new XElement("all-attributes"));
            }
            else
            {
                foreach (var col in link.Columns.Columns)
                    el.Add(new XElement("attribute", new XAttribute("name", col)));
            }

            if (link.LinkCriteria != null &&
                (link.LinkCriteria.Conditions.Count > 0 || link.LinkCriteria.Filters.Count > 0))
            {
                el.Add(ConvertFilter(link.LinkCriteria));
            }

            foreach (var nested in link.LinkEntities)
                el.Add(ConvertLinkEntity(nested));

            return el;
        }

        private static string GetOperatorString(ConditionOperator op)
        {
            switch (op)
            {
                case ConditionOperator.Equal: return "eq";
                case ConditionOperator.NotEqual: return "ne";
                case ConditionOperator.GreaterThan: return "gt";
                case ConditionOperator.GreaterEqual: return "ge";
                case ConditionOperator.LessThan: return "lt";
                case ConditionOperator.LessEqual: return "le";
                case ConditionOperator.Like: return "like";
                case ConditionOperator.NotLike: return "not-like";
                case ConditionOperator.In: return "in";
                case ConditionOperator.NotIn: return "not-in";
                case ConditionOperator.Null: return "null";
                case ConditionOperator.NotNull: return "not-null";
                case ConditionOperator.Between: return "between";
                case ConditionOperator.NotBetween: return "not-between";
                case ConditionOperator.Contains: return "like";
                case ConditionOperator.DoesNotContain: return "not-like";
                case ConditionOperator.BeginsWith: return "begins-with";
                case ConditionOperator.DoesNotBeginWith: return "not-begin-with";
                case ConditionOperator.EndsWith: return "ends-with";
                case ConditionOperator.DoesNotEndWith: return "not-end-with";
                default: return "eq";
            }
        }
    }
}
