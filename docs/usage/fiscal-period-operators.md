# Advanced Fiscal Period Operators

## Overview

Fiscal period operators enable date-based queries using fiscal calendars instead of standard calendar dates. This is essential for financial reporting, budgeting, and any scenario where your organization's fiscal year differs from the calendar year.

**Implemented:** 2025-10-10 (Issue #3)

## Microsoft Documentation

Official reference: [ConditionOperator Enum](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator)

## Supported Operators

| Operator | Description |
|----------|-------------|
| `InFiscalPeriod` | Within a specific fiscal period |
| `InFiscalPeriodAndYear` | Within a specific fiscal period and year |
| `InOrBeforeFiscalPeriodAndYear` | On or before a specific fiscal period and year |
| `InOrAfterFiscalPeriodAndYear` | On or after a specific fiscal period and year |
| `LastFiscalPeriod` | Previous fiscal period |
| `LastFiscalYear` | Previous fiscal year |
| `NextFiscalPeriod` | Next fiscal period |
| `NextFiscalYear` | Next fiscal year |

## Fiscal Year Configuration

Before using fiscal period operators, configure the fiscal year settings:

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Configure fiscal year (July 1 start, quarterly periods)
var fiscalSettings = new FiscalYearSettings
{
    StartDate = new DateTime(DateTime.Today.Year, 7, 1), // July 1
    PeriodType = FiscalPeriodType.Quarterly, // 4 periods per year
    FiscalYearDisplayCode = 0 // Display as calendar year
};

context.SetProperty(fiscalSettings);
```

### Fiscal Period Types

```csharp
public enum FiscalPeriodType
{
    Monthly = 1,        // 12 periods per year
    Quarterly = 2,      // 4 periods per year
    SemiAnnually = 3,   // 2 periods per year
    Annually = 4        // 1 period per year
}
```

## Usage Examples

### InFiscalPeriod Operator

Query records within a specific fiscal period:

```csharp
// Create test data with various dates
var currentYear = DateTime.Today.Year;
var q1Start = new DateTime(currentYear, 7, 1);   // Q1: Jul-Sep
var q2Start = new DateTime(currentYear, 10, 1);  // Q2: Oct-Dec
var q3Start = new DateTime(currentYear + 1, 1, 1); // Q3: Jan-Mar

var opp1 = new Entity("opportunity")
{
    Id = Guid.NewGuid(),
    ["estimatedclosedate"] = q1Start.AddDays(15) // In Q1
};

var opp2 = new Entity("opportunity")
{
    Id = Guid.NewGuid(),
    ["estimatedclosedate"] = q2Start.AddDays(15) // In Q2
};

context.Initialize(new[] { opp1, opp2 });

// Query: Find opportunities closing in fiscal period 1 (Q1)
var query = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("estimatedclosedate"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("estimatedclosedate", ConditionOperator.InFiscalPeriod, 1)
        }
    }
};

var results = service.RetrieveMultiple(query);
// Returns: opp1 (closes in Q1)
```

### InFiscalPeriodAndYear Operator

Query records within a specific fiscal period and year:

```csharp
// Query: Find opportunities in Q2 of fiscal year 2025
var query = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("estimatedclosedate"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("estimatedclosedate", 
                ConditionOperator.InFiscalPeriodAndYear, 
                2,    // Period (Q2)
                2025) // Fiscal Year
        }
    }
};

var results = service.RetrieveMultiple(query);
```

### LastFiscalPeriod Operator

Query records from the previous fiscal period:

```csharp
// Setup fiscal calendar with monthly periods
var fiscalSettings = new FiscalYearSettings
{
    StartDate = new DateTime(DateTime.Today.Year, 1, 1),
    PeriodType = FiscalPeriodType.Monthly
};
context.SetProperty(fiscalSettings);

// Query: Find invoices from last fiscal period (last month)
var query = new QueryExpression("invoice")
{
    ColumnSet = new ColumnSet("createdon"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("createdon", ConditionOperator.LastFiscalPeriod)
        }
    }
};

var results = service.RetrieveMultiple(query);
// Returns all invoices created in the previous fiscal period
```

### LastFiscalYear Operator

Query records from the previous fiscal year:

```csharp
// Query: Find all sales from last fiscal year
var query = new QueryExpression("salesorder")
{
    ColumnSet = new ColumnSet("totalamount", "createdon"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("createdon", ConditionOperator.LastFiscalYear)
        }
    }
};

var results = service.RetrieveMultiple(query);
var totalSales = results.Entities
    .Sum(e => e.GetAttributeValue<Money>("totalamount")?.Value ?? 0);
```

### NextFiscalPeriod Operator

Query records in the next fiscal period:

```csharp
// Query: Find opportunities expected to close next fiscal period
var query = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("name", "estimatedclosedate"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("estimatedclosedate", ConditionOperator.NextFiscalPeriod)
        }
    }
};

var results = service.RetrieveMultiple(query);
```

### NextFiscalYear Operator

Query records in the next fiscal year:

```csharp
// Query: Find contracts renewing next fiscal year
var query = new QueryExpression("contract")
{
    ColumnSet = new ColumnSet("title", "expirationdate"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("expirationdate", ConditionOperator.NextFiscalYear)
        }
    }
};

var results = service.RetrieveMultiple(query);
```

### InOrBeforeFiscalPeriodAndYear Operator

Query records on or before a specific fiscal period:

```csharp
// Query: Find all invoices up to and including Q2 2025
var query = new QueryExpression("invoice")
{
    ColumnSet = new ColumnSet("invoicedate"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("invoicedate", 
                ConditionOperator.InOrBeforeFiscalPeriodAndYear,
                2,    // Q2
                2025) // Fiscal Year 2025
        }
    }
};

var results = service.RetrieveMultiple(query);
```

### InOrAfterFiscalPeriodAndYear Operator

Query records on or after a specific fiscal period:

```csharp
// Query: Find all opportunities from Q3 2025 onwards
var query = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("estimatedclosedate"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("estimatedclosedate", 
                ConditionOperator.InOrAfterFiscalPeriodAndYear,
                3,    // Q3
                2025) // Fiscal Year 2025
        }
    }
};

var results = service.RetrieveMultiple(query);
```

## FetchXML Support

All fiscal period operators are supported in FetchXML:

### LastFiscalYear in FetchXML
```csharp
var fetchXml = @"
<fetch>
    <entity name='opportunity'>
        <attribute name='name' />
        <attribute name='estimatedvalue' />
        <filter>
            <condition attribute='actualclosedate' operator='last-fiscal-year' />
        </filter>
    </entity>
</fetch>";

var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

### InFiscalPeriodAndYear in FetchXML
```csharp
var fetchXml = @"
<fetch>
    <entity name='invoice'>
        <attribute name='totalamount' />
        <filter>
            <condition attribute='invoicedate' 
                       operator='in-fiscal-period-and-year' 
                       value='2' 
                       valueof='2025' />
        </filter>
    </entity>
</fetch>";

var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

## Advanced Scenarios

### Multiple Fiscal Period Conditions

```csharp
// Find opportunities closing in Q1 OR Q2 of current fiscal year
var query = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("name", "estimatedclosedate"),
    Criteria = new FilterExpression
    {
        FilterOperator = LogicalOperator.Or,
        Conditions =
        {
            new ConditionExpression("estimatedclosedate", ConditionOperator.InFiscalPeriod, 1),
            new ConditionExpression("estimatedclosedate", ConditionOperator.InFiscalPeriod, 2)
        }
    }
};

var results = service.RetrieveMultiple(query);
```

### Combining with Other Operators

```csharp
// Find high-value opportunities closing next fiscal period
var query = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("name", "estimatedvalue", "estimatedclosedate"),
    Criteria = new FilterExpression
    {
        FilterOperator = LogicalOperator.And,
        Conditions =
        {
            new ConditionExpression("estimatedclosedate", ConditionOperator.NextFiscalPeriod),
            new ConditionExpression("estimatedvalue", ConditionOperator.GreaterThan, 100000)
        }
    }
};

var results = service.RetrieveMultiple(query);
```

### Year-over-Year Comparison

```csharp
// Get revenue for last fiscal year
var lastYearQuery = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("actualvalue"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("actualclosedate", ConditionOperator.LastFiscalYear),
            new ConditionExpression("statecode", ConditionOperator.Equal, 1) // Won
        }
    }
};

var lastYearRevenue = service.RetrieveMultiple(lastYearQuery).Entities
    .Sum(e => e.GetAttributeValue<Money>("actualvalue")?.Value ?? 0);

// Get revenue for current fiscal year to date
var thisYearQuery = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("actualvalue"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("actualclosedate", ConditionOperator.ThisFiscalYear),
            new ConditionExpression("statecode", ConditionOperator.Equal, 1) // Won
        }
    }
};

var thisYearRevenue = service.RetrieveMultiple(thisYearQuery).Entities
    .Sum(e => e.GetAttributeValue<Money>("actualvalue")?.Value ?? 0);

var growthRate = ((thisYearRevenue - lastYearRevenue) / lastYearRevenue) * 100;
```

## Fiscal Calendar Configurations

### Standard July 1 Fiscal Year (Quarterly)

```csharp
var settings = new FiscalYearSettings
{
    StartDate = new DateTime(DateTime.Today.Year, 7, 1),
    PeriodType = FiscalPeriodType.Quarterly
};
// Q1: Jul-Sep, Q2: Oct-Dec, Q3: Jan-Mar, Q4: Apr-Jun
```

### Calendar Year (Monthly)

```csharp
var settings = new FiscalYearSettings
{
    StartDate = new DateTime(DateTime.Today.Year, 1, 1),
    PeriodType = FiscalPeriodType.Monthly
};
// Period 1: Jan, Period 2: Feb, ..., Period 12: Dec
```

### April 1 Fiscal Year (Semi-Annual)

```csharp
var settings = new FiscalYearSettings
{
    StartDate = new DateTime(DateTime.Today.Year, 4, 1),
    PeriodType = FiscalPeriodType.SemiAnnually
};
// Period 1: Apr-Sep, Period 2: Oct-Mar
```

### Custom Fiscal Year Start

```csharp
var settings = new FiscalYearSettings
{
    StartDate = new DateTime(DateTime.Today.Year, 10, 1), // October 1
    PeriodType = FiscalPeriodType.Quarterly
};
// Q1: Oct-Dec, Q2: Jan-Mar, Q3: Apr-Jun, Q4: Jul-Sep
```

## Period Calculation Examples

### Quarterly Periods (4 per year)
- Period 1: Months 1-3 (from fiscal year start)
- Period 2: Months 4-6
- Period 3: Months 7-9
- Period 4: Months 10-12

### Monthly Periods (12 per year)
- Period 1: Month 1 (from fiscal year start)
- Period 2: Month 2
- ...
- Period 12: Month 12

### Semi-Annual Periods (2 per year)
- Period 1: Months 1-6 (from fiscal year start)
- Period 2: Months 7-12

## Common Use Cases

### Financial Reporting

```csharp
// Q4 revenue report
var query = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("actualvalue", "actualclosedate"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("actualclosedate", ConditionOperator.InFiscalPeriod, 4),
            new ConditionExpression("statecode", ConditionOperator.Equal, 1) // Won
        }
    }
};

var q4Opportunities = service.RetrieveMultiple(query);
var q4Revenue = q4Opportunities.Entities
    .Sum(e => e.GetAttributeValue<Money>("actualvalue")?.Value ?? 0);
```

### Budget Tracking

```csharp
// Track spending against fiscal year budget
var query = new QueryExpression("invoice")
{
    ColumnSet = new ColumnSet("totalamount"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("invoicedate", ConditionOperator.ThisFiscalYear)
        }
    }
};

var yearToDateSpend = service.RetrieveMultiple(query).Entities
    .Sum(e => e.GetAttributeValue<Money>("totalamount")?.Value ?? 0);
```

### Sales Forecasting

```csharp
// Forecast pipeline for next fiscal period
var query = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("name", "estimatedvalue", "closeprobability"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("estimatedclosedate", ConditionOperator.NextFiscalPeriod),
            new ConditionExpression("statecode", ConditionOperator.Equal, 0) // Open
        }
    }
};

var pipeline = service.RetrieveMultiple(query);
var weightedPipeline = pipeline.Entities.Sum(e => 
    (e.GetAttributeValue<Money>("estimatedvalue")?.Value ?? 0) * 
    (e.GetAttributeValue<int>("closeprobability") / 100.0m));
```

## Best Practices

1. **Configure Fiscal Settings Early**: Set fiscal year settings before running queries
2. **Understand Period Boundaries**: Be aware of how periods map to calendar months
3. **Use Appropriate Period Type**: Choose monthly, quarterly, semi-annual, or annual based on reporting needs
4. **Test with Real Dates**: Ensure queries work correctly across fiscal year boundaries
5. **Document Fiscal Year**: Clearly document your organization's fiscal year configuration

## Error Scenarios

### Missing Fiscal Configuration

```csharp
// Querying without setting fiscal calendar
var query = new QueryExpression("opportunity")
{
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("estimatedclosedate", ConditionOperator.InFiscalPeriod, 1)
        }
    }
};

// Uses default: Calendar year (Jan 1), Quarterly periods
var results = service.RetrieveMultiple(query);
```

### Invalid Period Number

```csharp
// Requesting period 5 with quarterly fiscal year (only 4 periods)
var query = new QueryExpression("opportunity")
{
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("estimatedclosedate", ConditionOperator.InFiscalPeriod, 5)
        }
    }
};

// Returns empty result set
var results = service.RetrieveMultiple(query);
```

## Related Documentation

- [Microsoft ConditionOperator Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator)
- [Fiscal Calendar in Dataverse](https://learn.microsoft.com/en-us/power-platform/admin/work-fiscal-year-settings)
- [Query Expression Guide](../Fake4DataverseCore/README.md#query-expressions)

## Implementation Details

- **Files**: 
  - `Fake4DataverseCore/Fake4Dataverse.Core/Query/ConditionExpressionExtensions.FiscalPeriod.cs`
  - `Fake4DataverseCore/Fake4Dataverse.Core/Extensions/XmlExtensionsForFetchXml.cs`
- **Tests**: `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/FakeContextTests/FetchXml/FiscalPeriodOperatorTests.cs`
