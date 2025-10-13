using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System;
using System.Linq;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions;

namespace Fake4Dataverse.FakeMessageExecutors
{
    /// <summary>
    /// Executor for RetrieveDuplicatesRequest message
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveduplicatesrequest
    /// 
    /// RetrieveDuplicatesRequest detects and retrieves duplicate records for a specified record based on 
    /// duplicate detection rules defined in Dataverse. The message evaluates active and published duplicate 
    /// detection rules (duplicaterule entity) for the target entity type and returns matching records.
    /// 
    /// Key Properties:
    /// - BusinessEntity: The entity record to check for duplicates
    /// - MatchingEntityName: The entity type to search for duplicates
    /// - PagingInfo: Optional paging parameters for the results
    /// 
    /// The duplicate detection logic evaluates conditions defined in duplicaterulecondition records
    /// associated with active and published duplicaterule records.
    /// </summary>
    public class RetrieveDuplicatesRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveDuplicatesRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var req = request as RetrieveDuplicatesRequest;
            var context = ctx as XrmFakedContext;

            if (req.BusinessEntity == null)
            {
                throw new ArgumentNullException(nameof(req.BusinessEntity), "BusinessEntity property is required");
            }

            if (string.IsNullOrWhiteSpace(req.MatchingEntityName))
            {
                throw new ArgumentNullException(nameof(req.MatchingEntityName), "MatchingEntityName property is required");
            }

            var baseEntityName = req.BusinessEntity.LogicalName;
            var matchingEntityName = req.MatchingEntityName;

            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/detect-duplicate-data
            // Duplicate detection rules are stored in the duplicaterule entity
            // Only active and published rules (statecode = 0, statuscode = 2) are evaluated
            
            var duplicateCollection = new EntityCollection();

            // Check if duplicate detection rules exist for this entity combination
            if (!context.Data.ContainsKey("duplicaterule"))
            {
                // No duplicate rules defined, return empty collection
                return new RetrieveDuplicatesResponse
                {
                    Results = new ParameterCollection
                    {
                        { "DuplicateCollection", duplicateCollection }
                    }
                };
            }

            var applicableRules = context.Data["duplicaterule"].Values
                .Where(r => 
                    r.Contains("baseentityname") && 
                    r.GetAttributeValue<string>("baseentityname") == baseEntityName &&
                    r.Contains("matchingentityname") &&
                    r.GetAttributeValue<string>("matchingentityname") == matchingEntityName &&
                    // Only active and published rules (statecode = 0 for active, statuscode = 2 for published)
                    r.Contains("statecode") &&
                    r.GetAttributeValue<OptionSetValue>("statecode")?.Value == 0 &&
                    r.Contains("statuscode") &&
                    r.GetAttributeValue<OptionSetValue>("statuscode")?.Value == 2
                )
                .ToList();

            if (!applicableRules.Any())
            {
                // No active rules for this entity combination
                return new RetrieveDuplicatesResponse
                {
                    Results = new ParameterCollection
                    {
                        { "DuplicateCollection", duplicateCollection }
                    }
                };
            }

            // Check if matching entities exist in context
            if (!context.Data.ContainsKey(matchingEntityName))
            {
                return new RetrieveDuplicatesResponse
                {
                    Results = new ParameterCollection
                    {
                        { "DuplicateCollection", duplicateCollection }
                    }
                };
            }

            // Evaluate each rule to find duplicates
            foreach (var rule in applicableRules)
            {
                var ruleId = rule.Id;

                // Get conditions for this rule from duplicaterulecondition entity
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
                // duplicaterulecondition defines the comparison criteria between base and matching attributes
                if (!context.Data.ContainsKey("duplicaterulecondition"))
                {
                    continue;
                }

                var ruleConditions = context.Data["duplicaterulecondition"].Values
                    .Where(c => 
                        c.Contains("duplicateruleid") &&
                        c.GetAttributeValue<EntityReference>("duplicateruleid")?.Id == ruleId
                    )
                    .ToList();

                if (!ruleConditions.Any())
                {
                    continue;
                }

                // Find matching records based on rule conditions
                var matchingRecords = context.Data[matchingEntityName].Values
                    .Where(candidate => 
                    {
                        // Exclude the same record if checking within the same entity
                        if (baseEntityName == matchingEntityName && 
                            req.BusinessEntity.Id != Guid.Empty && 
                            candidate.Id == req.BusinessEntity.Id)
                        {
                            return false;
                        }

                        // All conditions must match for a record to be considered a duplicate
                        return ruleConditions.All(condition => 
                            EvaluateCondition(req.BusinessEntity, candidate, condition)
                        );
                    })
                    .ToList();

                // Add matching records to duplicate collection
                foreach (var match in matchingRecords)
                {
                    // Avoid adding the same duplicate multiple times
                    if (!duplicateCollection.Entities.Any(e => e.Id == match.Id))
                    {
                        duplicateCollection.Entities.Add(match);
                    }
                }
            }

            return new RetrieveDuplicatesResponse
            {
                Results = new ParameterCollection
                {
                    { "DuplicateCollection", duplicateCollection }
                }
            };
        }

        /// <summary>
        /// Evaluates a single duplicate detection condition
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
        /// 
        /// The operatorcode attribute defines the comparison type:
        /// - 0 (ExactMatch): Values must be exactly the same
        /// - 1 (SameFirstCharacters): Beginning characters must match (uses ignoreblanks for character count)
        /// - 2 (SameLastCharacters): Ending characters must match (uses ignoreblanks for character count)
        /// - 3 (SameDate): Date values must match (ignoring time)
        /// - 4 (SameDateAndTime): Date and time must match
        /// - 5 (SameNotBlank): Both values must be non-blank and match
        /// 
        /// For SameFirstCharacters and SameLastCharacters, the ignoreblanks attribute specifies
        /// the number of characters to compare. If not specified, compares entire strings.
        /// </summary>
        private bool EvaluateCondition(Entity baseEntity, Entity matchingEntity, Entity condition)
        {
            var baseAttributeName = condition.GetAttributeValue<string>("baseattributename");
            var matchingAttributeName = condition.GetAttributeValue<string>("matchingattributename");
            var operatorCode = condition.Contains("operatorcode") 
                ? condition.GetAttributeValue<OptionSetValue>("operatorcode")?.Value ?? 0 
                : 0; // Default to exact match

            if (string.IsNullOrWhiteSpace(baseAttributeName) || 
                string.IsNullOrWhiteSpace(matchingAttributeName))
            {
                return false;
            }

            // Get attribute values
            var baseValue = baseEntity.Contains(baseAttributeName) ? baseEntity[baseAttributeName] : null;
            var matchingValue = matchingEntity.Contains(matchingAttributeName) ? matchingEntity[matchingAttributeName] : null;

            // Handle null values - nulls don't match anything
            if (baseValue == null || matchingValue == null)
            {
                return false;
            }

            // Convert to strings for comparison
            var baseStr = ConvertToString(baseValue);
            var matchingStr = ConvertToString(matchingValue);

            // Evaluate based on operator code
            switch (operatorCode)
            {
                case 0: // ExactMatch
                    return string.Equals(baseStr, matchingStr, StringComparison.OrdinalIgnoreCase);
                
                case 1: // SameFirstCharacters
                    return CompareSameFirstCharacters(baseStr, matchingStr, condition);
                
                case 2: // SameLastCharacters
                    return CompareSameLastCharacters(baseStr, matchingStr, condition);
                
                case 3: // SameDate
                    if (baseValue is DateTime baseDate && matchingValue is DateTime matchingDate)
                    {
                        return baseDate.Date == matchingDate.Date;
                    }
                    return false;
                
                case 4: // SameDateAndTime
                    if (baseValue is DateTime baseDT && matchingValue is DateTime matchingDT)
                    {
                        return baseDT == matchingDT;
                    }
                    return false;
                
                case 5: // SameNotBlank
                    if (!string.IsNullOrWhiteSpace(baseStr) && !string.IsNullOrWhiteSpace(matchingStr))
                    {
                        return string.Equals(baseStr, matchingStr, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                
                default:
                    // Unknown operator, default to exact match
                    return string.Equals(baseStr, matchingStr, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Compares the first N characters of two strings
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
        /// 
        /// The ignoreblanks attribute specifies the number of characters to compare.
        /// If not specified, compares the entire strings.
        /// </summary>
        private bool CompareSameFirstCharacters(string baseStr, string matchingStr, Entity condition)
        {
            // Get the number of characters to compare from ignoreblanks attribute
            int? charCount = null;
            if (condition.Contains("ignoreblanks"))
            {
                var ignoreblanks = condition.GetAttributeValue<int?>("ignoreblanks");
                if (ignoreblanks.HasValue && ignoreblanks.Value > 0)
                {
                    charCount = ignoreblanks.Value;
                }
            }

            if (charCount.HasValue)
            {
                // Compare only the first N characters
                var baseSubstr = baseStr.Length >= charCount.Value 
                    ? baseStr.Substring(0, charCount.Value) 
                    : baseStr;
                var matchingSubstr = matchingStr.Length >= charCount.Value 
                    ? matchingStr.Substring(0, charCount.Value) 
                    : matchingStr;
                
                return string.Equals(baseSubstr, matchingSubstr, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // If no character count specified, compare entire strings
                return string.Equals(baseStr, matchingStr, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Compares the last N characters of two strings
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
        /// 
        /// The ignoreblanks attribute specifies the number of characters to compare.
        /// If not specified, compares the entire strings.
        /// </summary>
        private bool CompareSameLastCharacters(string baseStr, string matchingStr, Entity condition)
        {
            // Get the number of characters to compare from ignoreblanks attribute
            int? charCount = null;
            if (condition.Contains("ignoreblanks"))
            {
                var ignoreblanks = condition.GetAttributeValue<int?>("ignoreblanks");
                if (ignoreblanks.HasValue && ignoreblanks.Value > 0)
                {
                    charCount = ignoreblanks.Value;
                }
            }

            if (charCount.HasValue)
            {
                // Compare only the last N characters
                var baseSubstr = baseStr.Length >= charCount.Value 
                    ? baseStr.Substring(baseStr.Length - charCount.Value) 
                    : baseStr;
                var matchingSubstr = matchingStr.Length >= charCount.Value 
                    ? matchingStr.Substring(matchingStr.Length - charCount.Value) 
                    : matchingStr;
                
                return string.Equals(baseSubstr, matchingSubstr, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // If no character count specified, compare entire strings
                return string.Equals(baseStr, matchingStr, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Converts various attribute value types to strings for comparison
        /// </summary>
        private string ConvertToString(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is string str)
            {
                return str;
            }

            if (value is OptionSetValue optionSet)
            {
                return optionSet.Value.ToString();
            }

            if (value is EntityReference entityRef)
            {
                return entityRef.Id.ToString();
            }

            if (value is Money money)
            {
                return money.Value.ToString();
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("o"); // ISO 8601 format
            }

            return value.ToString();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveDuplicatesRequest);
        }
    }
}
