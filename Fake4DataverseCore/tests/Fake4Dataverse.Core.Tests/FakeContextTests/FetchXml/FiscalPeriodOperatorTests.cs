using System;
using System.Linq;
using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Settings;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.FetchXml
{
    /// <summary>
    /// Tests for fiscal period condition operators
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/fiscal-date-older-datetime-query-operators-fetchxml
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
    /// 
    /// Fiscal date and time query operators enable querying date and time values using fiscal periods defined in the
    /// organization's fiscal year settings. These operators are particularly useful for financial reporting where the
    /// fiscal year may not align with the calendar year.
    /// 
    /// The fiscal calendar settings include:
    /// - Fiscal year start date (e.g., April 1, July 1, October 1)
    /// - Fiscal period template: Annually (1 period), Semi-Annually (2 periods), Quarterly (4 periods), 
    ///   Monthly (12 periods), or Four-Week (13 periods)
    /// </summary>
    public class FiscalPeriodOperatorTests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public FiscalPeriodOperatorTests()
        {
            _context = XrmFakedContextFactory.New();
            _service = _context.GetOrganizationService();
        }
        [Fact]
        public void FetchXml_Operator_InFiscalPeriod_Quarterly_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            // InFiscalPeriod: The value is within the specified fiscal period.
            // This test verifies that records are correctly filtered to only those within Q2 (fiscal period 2)
            // of a quarterly fiscal calendar starting January 1.
            var today = DateTime.Today;
            var currentYear = today.Year;
            
            // Set up quarterly fiscal calendar starting January 1
            _context.SetProperty<FiscalYearSettings>(new FiscalYearSettings() 
            { 
                StartDate = new DateTime(currentYear, 1, 1), 
                FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly 
            });

            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='anniversary' />
                                        <filter type='and'>
                                            <condition attribute='anniversary' operator='in-fiscal-period' value='2' />
                                        </filter>
                                  </entity>
                            </fetch>";

            // Q2 is approximately April-June (days 91-181)
            var ct1 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 4, 15) };  // Q2 - should be returned
            var ct2 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 5, 20) };  // Q2 - should be returned
            var ct3 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 2, 10) };  // Q1 - should not be returned
            var ct4 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 7, 5) };   // Q3 - should not be returned
            _context.Initialize(new[] { ct1, ct2, ct3, ct4 });

            var collection = _service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Equal(2, collection.Entities.Count);
            Assert.Contains(collection.Entities, e => e.Id == ct1.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct2.Id);
        }

        [Fact]
        public void FetchXml_Operator_InFiscalPeriodAndYear_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            // InFiscalPeriodAndYear: The value is within the specified fiscal period and fiscal year.
            // This test verifies filtering for records in fiscal period 1 (Q1) of a specific fiscal year
            // when using a quarterly fiscal calendar.
            var today = DateTime.Today;
            var currentYear = today.Year;
            var targetYear = currentYear - 1;
            
            // Set up quarterly fiscal calendar
            _context.SetProperty<FiscalYearSettings>(new FiscalYearSettings() 
            { 
                StartDate = new DateTime(currentYear, 1, 1), 
                FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly 
            });

            // For FetchXML, we need to use QueryExpression as FetchXML doesn't support this operator properly in XML
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("anniversary"),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("anniversary", ConditionOperator.InFiscalPeriodAndYear, 1, targetYear);

            // Q1 of target year (approximately Jan-Mar)
            var ct1 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(targetYear, 1, 15) };  // Should be returned
            var ct2 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(targetYear, 2, 20) };  // Should be returned
            var ct3 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 1, 10) }; // Wrong year - should not be returned
            var ct4 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(targetYear, 5, 5) };   // Q2 - should not be returned
            _context.Initialize(new[] { ct1, ct2, ct3, ct4 });

            var collection = _service.RetrieveMultiple(query);

            Assert.Equal(2, collection.Entities.Count);
            Assert.Contains(collection.Entities, e => e.Id == ct1.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct2.Id);
        }

        [Fact]
        public void FetchXml_Operator_LastFiscalPeriod_Monthly_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            // LastFiscalPeriod: The value is within the last fiscal period. This is calculated relative to the
            // current date. For a monthly fiscal calendar, this returns records from the previous month
            // of the fiscal calendar.
            var today = DateTime.Today;
            var currentYear = today.Year;
            
            // Set up monthly fiscal calendar
            _context.SetProperty<FiscalYearSettings>(new FiscalYearSettings() 
            { 
                StartDate = new DateTime(currentYear, 1, 1), 
                FiscalPeriodTemplate = FiscalYearSettings.Template.Monthly 
            });

            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='anniversary' />
                                        <filter type='and'>
                                            <condition attribute='anniversary' operator='last-fiscal-period' />
                                        </filter>
                                  </entity>
                            </fetch>";

            // Calculate last fiscal period dates
            var fiscalYearStart = new DateTime(currentYear, 1, 1);
            var daysSinceStart = (today - fiscalYearStart).Days;
            var currentPeriod = daysSinceStart / 30 + 1;
            var lastPeriod = currentPeriod > 1 ? currentPeriod - 1 : 12;
            var lastPeriodYear = currentPeriod > 1 ? currentYear : currentYear - 1;

            var lastPeriodStart = new DateTime(lastPeriodYear, 1, 1).AddDays((lastPeriod - 1) * 30);
            var lastPeriodEnd = lastPeriodStart.AddDays(29);

            var ct1 = new Contact() { Id = Guid.NewGuid(), Anniversary = lastPeriodStart };
            var ct2 = new Contact() { Id = Guid.NewGuid(), Anniversary = lastPeriodEnd };
            var ct3 = new Contact() { Id = Guid.NewGuid(), Anniversary = today }; // Current period - should not be returned
            _context.Initialize(new[] { ct1, ct2, ct3 });

            var collection = _service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Equal(2, collection.Entities.Count);
            Assert.Contains(collection.Entities, e => e.Id == ct1.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct2.Id);
        }

        [Fact]
        public void FetchXml_Operator_NextFiscalPeriod_Quarterly_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            // NextFiscalPeriod: The value is within the next fiscal period. This is calculated relative to the
            // current date. For a quarterly fiscal calendar, this returns records from the next quarter.
            var today = DateTime.Today;
            var currentYear = today.Year;
            
            // Set up quarterly fiscal calendar
            _context.SetProperty<FiscalYearSettings>(new FiscalYearSettings() 
            { 
                StartDate = new DateTime(currentYear, 1, 1), 
                FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly 
            });

            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='anniversary' />
                                        <filter type='and'>
                                            <condition attribute='anniversary' operator='next-fiscal-period' />
                                        </filter>
                                  </entity>
                            </fetch>";

            // Calculate next fiscal period dates
            var fiscalYearStart = new DateTime(currentYear, 1, 1);
            var daysSinceStart = (today - fiscalYearStart).Days;
            var currentPeriod = daysSinceStart / 91 + 1; // ~3 months per quarter
            var nextPeriod = currentPeriod < 4 ? currentPeriod + 1 : 1;
            var nextPeriodYear = currentPeriod < 4 ? currentYear : currentYear + 1;

            var nextPeriodStart = new DateTime(nextPeriodYear, 1, 1).AddDays((nextPeriod - 1) * 91);
            var nextPeriodEnd = nextPeriodStart.AddDays(90);

            var ct1 = new Contact() { Id = Guid.NewGuid(), Anniversary = nextPeriodStart };
            var ct2 = new Contact() { Id = Guid.NewGuid(), Anniversary = nextPeriodEnd };
            var ct3 = new Contact() { Id = Guid.NewGuid(), Anniversary = today }; // Current period - should not be returned
            _context.Initialize(new[] { ct1, ct2, ct3 });

            var collection = _service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Equal(2, collection.Entities.Count);
            Assert.Contains(collection.Entities, e => e.Id == ct1.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct2.Id);
        }

        [Fact]
        public void FetchXml_Operator_LastFiscalYear_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            // LastFiscalYear: The value is within the last fiscal year. This is calculated relative to the
            // current date and respects custom fiscal year start dates (e.g., April 1 fiscal year start).
            var today = DateTime.Today;
            var currentYear = today.Year;
            
            // Set up fiscal calendar with April 1 start
            var fiscalStartDate = new DateTime(currentYear, 4, 1);
            _context.SetProperty<FiscalYearSettings>(new FiscalYearSettings() 
            { 
                StartDate = fiscalStartDate, 
                FiscalPeriodTemplate = FiscalYearSettings.Template.Annually 
            });

            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='anniversary' />
                                        <filter type='and'>
                                            <condition attribute='anniversary' operator='last-fiscal-year' />
                                        </filter>
                                  </entity>
                            </fetch>";

            // Calculate last fiscal year
            var currentFiscalYear = today >= new DateTime(today.Year, 4, 1) ? today.Year : today.Year - 1;
            var lastFiscalYearStart = new DateTime(currentFiscalYear - 1, 4, 1);
            var lastFiscalYearEnd = new DateTime(currentFiscalYear, 3, 31);

            var ct1 = new Contact() { Id = Guid.NewGuid(), Anniversary = lastFiscalYearStart };
            var ct2 = new Contact() { Id = Guid.NewGuid(), Anniversary = lastFiscalYearEnd };
            var ct3 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentFiscalYear, 5, 1) }; // Current fiscal year
            var ct4 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentFiscalYear - 2, 5, 1) }; // Too old
            _context.Initialize(new[] { ct1, ct2, ct3, ct4 });

            var collection = _service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Equal(2, collection.Entities.Count);
            Assert.Contains(collection.Entities, e => e.Id == ct1.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct2.Id);
        }

        [Fact]
        public void FetchXml_Operator_NextFiscalYear_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            // NextFiscalYear: The value is within the next fiscal year. This is calculated relative to the
            // current date and respects custom fiscal year start dates (e.g., July 1 fiscal year start).
            var today = DateTime.Today;
            var currentYear = today.Year;
            
            // Set up fiscal calendar with July 1 start
            var fiscalStartDate = new DateTime(currentYear, 7, 1);
            _context.SetProperty<FiscalYearSettings>(new FiscalYearSettings() 
            { 
                StartDate = fiscalStartDate, 
                FiscalPeriodTemplate = FiscalYearSettings.Template.Annually 
            });

            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='anniversary' />
                                        <filter type='and'>
                                            <condition attribute='anniversary' operator='next-fiscal-year' />
                                        </filter>
                                  </entity>
                            </fetch>";

            // Calculate next fiscal year
            var currentFiscalYear = today >= new DateTime(today.Year, 7, 1) ? today.Year : today.Year - 1;
            var nextFiscalYearStart = new DateTime(currentFiscalYear + 1, 7, 1);
            var nextFiscalYearEnd = new DateTime(currentFiscalYear + 2, 6, 30);

            var ct1 = new Contact() { Id = Guid.NewGuid(), Anniversary = nextFiscalYearStart };
            var ct2 = new Contact() { Id = Guid.NewGuid(), Anniversary = nextFiscalYearEnd };
            var ct3 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentFiscalYear, 8, 1) }; // Current fiscal year
            var ct4 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentFiscalYear + 2, 8, 1) }; // Too far in future
            _context.Initialize(new[] { ct1, ct2, ct3, ct4 });

            var collection = _service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Equal(2, collection.Entities.Count);
            Assert.Contains(collection.Entities, e => e.Id == ct1.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct2.Id);
        }

        [Fact]
        public void FetchXml_Operator_InOrAfterFiscalPeriodAndYear_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            // InOrAfterFiscalPeriodAndYear: The value is within or after the specified fiscal period and fiscal year.
            // This test verifies that records on or after the start of Q2 (fiscal period 2) are returned.
            var today = DateTime.Today;
            var currentYear = today.Year;
            
            // Set up quarterly fiscal calendar
            _context.SetProperty<FiscalYearSettings>(new FiscalYearSettings() 
            { 
                StartDate = new DateTime(currentYear, 1, 1), 
                FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly 
            });

            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("anniversary"),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("anniversary", ConditionOperator.InOrAfterFiscalPeriodAndYear, 2, currentYear);

            // Q2 of current year and after (approximately day 91 onwards)
            var q2Start = new DateTime(currentYear, 1, 1).AddDays(91);
            var ct1 = new Contact() { Id = Guid.NewGuid(), Anniversary = q2Start };                              // Q2 start - should be returned
            var ct2 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 7, 1) };      // Q3 - should be returned
            var ct3 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear + 1, 1, 1) };  // Next year - should be returned
            var ct4 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 2, 1) };      // Q1 - should not be returned
            _context.Initialize(new[] { ct1, ct2, ct3, ct4 });

            var collection = _service.RetrieveMultiple(query);

            Assert.Equal(3, collection.Entities.Count);
            Assert.Contains(collection.Entities, e => e.Id == ct1.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct2.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct3.Id);
        }

        [Fact]
        public void FetchXml_Operator_InOrBeforeFiscalPeriodAndYear_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            // InOrBeforeFiscalPeriodAndYear: The value is within or before the specified fiscal period and fiscal year.
            // This test verifies that records on or before the end of Q2 (fiscal period 2) are returned.
            var today = DateTime.Today;
            var currentYear = today.Year;
            
            // Set up quarterly fiscal calendar
            _context.SetProperty<FiscalYearSettings>(new FiscalYearSettings() 
            { 
                StartDate = new DateTime(currentYear, 1, 1), 
                FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly 
            });

            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("anniversary"),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("anniversary", ConditionOperator.InOrBeforeFiscalPeriodAndYear, 2, currentYear);

            // Q2 of current year and before (approximately through day 181)
            var q2End = new DateTime(currentYear, 1, 1).AddDays(181);
            var ct1 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 1, 15) };      // Q1 - should be returned
            var ct2 = new Contact() { Id = Guid.NewGuid(), Anniversary = q2End };                                 // Q2 end - should be returned
            var ct3 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear - 1, 12, 31) }; // Previous year - should be returned
            var ct4 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 7, 1) };       // Q3 - should not be returned
            _context.Initialize(new[] { ct1, ct2, ct3, ct4 });

            var collection = _service.RetrieveMultiple(query);

            Assert.Equal(3, collection.Entities.Count);
            Assert.Contains(collection.Entities, e => e.Id == ct1.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct2.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct3.Id);
        }

        [Fact]
        public void QueryExpression_Operator_InFiscalPeriod_SemiAnnually_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            // InFiscalPeriod: The value is within the specified fiscal period.
            // This test verifies semi-annual fiscal periods (H1 and H2) where H2 is approximately July-December.
            var today = DateTime.Today;
            var currentYear = today.Year;
            
            // Set up semi-annual fiscal calendar
            _context.SetProperty<FiscalYearSettings>(new FiscalYearSettings() 
            { 
                StartDate = new DateTime(currentYear, 1, 1), 
                FiscalPeriodTemplate = FiscalYearSettings.Template.SemiAnnually 
            });

            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("anniversary"),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("anniversary", ConditionOperator.InFiscalPeriod, 2);

            // H2 is approximately July-December (days 182-365)
            var ct1 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 7, 1) };   // H2 - should be returned
            var ct2 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 12, 31) }; // H2 - should be returned
            var ct3 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentYear, 3, 15) };  // H1 - should not be returned
            _context.Initialize(new[] { ct1, ct2, ct3 });

            var collection = _service.RetrieveMultiple(query);

            Assert.Equal(2, collection.Entities.Count);
            Assert.Contains(collection.Entities, e => e.Id == ct1.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct2.Id);
        }

        [Fact]
        public void QueryExpression_Operator_LastFiscalYear_CustomStart_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            // LastFiscalYear: The value is within the last fiscal year.
            // This test verifies the operator works correctly with a custom fiscal year start date (October 1),
            // which is commonly used by governments and some organizations.
            var today = DateTime.Today;
            var currentYear = today.Year;
            
            // Set up fiscal calendar with October 1 start (common for many organizations)
            var fiscalStartDate = new DateTime(currentYear, 10, 1);
            _context.SetProperty<FiscalYearSettings>(new FiscalYearSettings() 
            { 
                StartDate = fiscalStartDate, 
                FiscalPeriodTemplate = FiscalYearSettings.Template.Annually 
            });

            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("anniversary"),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("anniversary", ConditionOperator.LastFiscalYear);

            // Calculate last fiscal year based on October 1 start
            var currentFiscalYear = today >= new DateTime(today.Year, 10, 1) ? today.Year : today.Year - 1;
            var lastFiscalYearStart = new DateTime(currentFiscalYear - 1, 10, 1);
            var lastFiscalYearEnd = new DateTime(currentFiscalYear, 9, 30);

            var ct1 = new Contact() { Id = Guid.NewGuid(), Anniversary = lastFiscalYearStart };
            var ct2 = new Contact() { Id = Guid.NewGuid(), Anniversary = lastFiscalYearEnd };
            var ct3 = new Contact() { Id = Guid.NewGuid(), Anniversary = new DateTime(currentFiscalYear, 11, 1) }; // Current fiscal year
            _context.Initialize(new[] { ct1, ct2, ct3 });

            var collection = _service.RetrieveMultiple(query);

            Assert.Equal(2, collection.Entities.Count);
            Assert.Contains(collection.Entities, e => e.Id == ct1.Id);
            Assert.Contains(collection.Entities, e => e.Id == ct2.Id);
        }
    }
}
