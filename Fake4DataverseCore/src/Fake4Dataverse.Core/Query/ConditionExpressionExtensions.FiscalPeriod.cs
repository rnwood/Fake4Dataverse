using System;
using System.Linq.Expressions;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Settings;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Query
{
    /// <summary>
    /// Extensions for fiscal period condition operators
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
    /// </summary>
    public static partial class ConditionExpressionExtensions
    {
        /// <summary>
        /// Takes a condition expression for fiscal period operators and translates it into a 'between two dates' expression
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/fiscal-date-older-datetime-query-operators-fetchxml
        /// </summary>
        internal static Expression ToFiscalPeriodExpression(this TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr, IXrmFakedContext context)
        {
            var c = tc.CondExpression;

            DateTime? fromDate = null;
            DateTime? toDate = null;

            var today = DateTime.Today;
            var fiscalSettings = context.GetProperty<FiscalYearSettings>() ?? new FiscalYearSettings();
            var fiscalStartDate = fiscalSettings.StartDate;
            var fiscalPeriodTemplate = fiscalSettings.FiscalPeriodTemplate;

            // Calculate current fiscal year based on fiscal start date
            var currentFiscalYear = today >= new DateTime(today.Year, fiscalStartDate.Month, fiscalStartDate.Day)
                ? today.Year
                : today.Year - 1;

            // Calculate current fiscal period based on the template
            var currentFiscalPeriod = GetCurrentFiscalPeriod(today, fiscalStartDate, fiscalPeriodTemplate);

            switch (c.Operator)
            {
                case ConditionOperator.InFiscalPeriod:
                    // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
                    var period = (int)c.Values[0];
                    var fiscalYearForPeriod = c.Values.Count > 1 ? (int)c.Values[1] : currentFiscalYear;
                    c.Values.Clear();
                    (fromDate, toDate) = GetFiscalPeriodDates(fiscalYearForPeriod, period, fiscalStartDate, fiscalPeriodTemplate);
                    break;

                case ConditionOperator.InFiscalPeriodAndYear:
                    // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
                    var periodAndYear = (int)c.Values[0];
                    var yearForPeriodAndYear = (int)c.Values[1];
                    c.Values.Clear();
                    (fromDate, toDate) = GetFiscalPeriodDates(yearForPeriodAndYear, periodAndYear, fiscalStartDate, fiscalPeriodTemplate);
                    break;

                case ConditionOperator.InOrAfterFiscalPeriodAndYear:
                    // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
                    var periodInOrAfter = (int)c.Values[0];
                    var yearInOrAfter = (int)c.Values[1];
                    c.Values.Clear();
                    (fromDate, _) = GetFiscalPeriodDates(yearInOrAfter, periodInOrAfter, fiscalStartDate, fiscalPeriodTemplate);
                    toDate = DateTime.MaxValue;
                    break;

                case ConditionOperator.InOrBeforeFiscalPeriodAndYear:
                    // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
                    var periodInOrBefore = (int)c.Values[0];
                    var yearInOrBefore = (int)c.Values[1];
                    c.Values.Clear();
                    fromDate = DateTime.MinValue;
                    (_, toDate) = GetFiscalPeriodDates(yearInOrBefore, periodInOrBefore, fiscalStartDate, fiscalPeriodTemplate);
                    break;

                case ConditionOperator.LastFiscalPeriod:
                    // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
                    c.Values.Clear();
                    var (lastPeriodYear, lastPeriod) = GetPreviousFiscalPeriod(currentFiscalYear, currentFiscalPeriod, fiscalStartDate, fiscalPeriodTemplate);
                    (fromDate, toDate) = GetFiscalPeriodDates(lastPeriodYear, lastPeriod, fiscalStartDate, fiscalPeriodTemplate);
                    break;

                case ConditionOperator.NextFiscalPeriod:
                    // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
                    c.Values.Clear();
                    var (nextPeriodYear, nextPeriod) = GetNextFiscalPeriod(currentFiscalYear, currentFiscalPeriod, fiscalStartDate, fiscalPeriodTemplate);
                    (fromDate, toDate) = GetFiscalPeriodDates(nextPeriodYear, nextPeriod, fiscalStartDate, fiscalPeriodTemplate);
                    break;

                case ConditionOperator.LastFiscalYear:
                    // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
                    c.Values.Clear();
                    fromDate = new DateTime(currentFiscalYear - 1, fiscalStartDate.Month, fiscalStartDate.Day);
                    toDate = fromDate.Value.AddYears(1).AddDays(-1);
                    break;

                case ConditionOperator.NextFiscalYear:
                    // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
                    c.Values.Clear();
                    fromDate = new DateTime(currentFiscalYear + 1, fiscalStartDate.Month, fiscalStartDate.Day);
                    toDate = fromDate.Value.AddYears(1).AddDays(-1);
                    break;
            }

            c.Values.Add(fromDate);
            c.Values.Add(toDate);

            return tc.ToBetweenExpression(getAttributeValueExpr, containsAttributeExpr);
        }

        /// <summary>
        /// Gets the current fiscal period based on the current date, fiscal start date, and template
        /// </summary>
        private static int GetCurrentFiscalPeriod(DateTime currentDate, DateTime fiscalStartDate, FiscalYearSettings.Template template)
        {
            // Calculate fiscal year start for the current or previous year
            var fiscalYearStart = currentDate >= new DateTime(currentDate.Year, fiscalStartDate.Month, fiscalStartDate.Day)
                ? new DateTime(currentDate.Year, fiscalStartDate.Month, fiscalStartDate.Day)
                : new DateTime(currentDate.Year - 1, fiscalStartDate.Month, fiscalStartDate.Day);

            var daysSinceFiscalYearStart = (currentDate - fiscalYearStart).Days;

            switch (template)
            {
                case FiscalYearSettings.Template.Annually:
                    return 1;
                case FiscalYearSettings.Template.SemiAnnually:
                    return daysSinceFiscalYearStart / 182 + 1; // ~6 months
                case FiscalYearSettings.Template.Quarterly:
                    return daysSinceFiscalYearStart / 91 + 1;     // ~3 months
                case FiscalYearSettings.Template.Monthly:
                    return daysSinceFiscalYearStart / 30 + 1;       // ~1 month
                case FiscalYearSettings.Template.FourWeek:
                    return daysSinceFiscalYearStart / 28 + 1;      // 4 weeks
                default:
                    return 1;
            }
        }

        /// <summary>
        /// Gets the start and end dates for a specific fiscal period
        /// </summary>
        private static (DateTime fromDate, DateTime toDate) GetFiscalPeriodDates(int fiscalYear, int period, DateTime fiscalStartDate, FiscalYearSettings.Template template)
        {
            var fiscalYearStart = new DateTime(fiscalYear, fiscalStartDate.Month, fiscalStartDate.Day);
            
            int periodDays;
            int periodsPerYear;
            
            switch (template)
            {
                case FiscalYearSettings.Template.Annually:
                    periodDays = 365;
                    periodsPerYear = 1;
                    break;
                case FiscalYearSettings.Template.SemiAnnually:
                    periodDays = 182;
                    periodsPerYear = 2;
                    break;
                case FiscalYearSettings.Template.Quarterly:
                    periodDays = 91;
                    periodsPerYear = 4;
                    break;
                case FiscalYearSettings.Template.Monthly:
                    periodDays = 30;
                    periodsPerYear = 12;
                    break;
                case FiscalYearSettings.Template.FourWeek:
                    periodDays = 28;
                    periodsPerYear = 13;
                    break;
                default:
                    periodDays = 365;
                    periodsPerYear = 1;
                    break;
            }

            // Ensure period is valid
            if (period < 1 || period > periodsPerYear)
            {
                throw new ArgumentException($"Invalid fiscal period {period} for template {template}. Valid range: 1-{periodsPerYear}");
            }

            var fromDate = fiscalYearStart.AddDays((period - 1) * periodDays);
            var toDate = fromDate.AddDays(periodDays - 1);

            return (fromDate, toDate);
        }

        /// <summary>
        /// Gets the previous fiscal period and year
        /// </summary>
        private static (int year, int period) GetPreviousFiscalPeriod(int currentYear, int currentPeriod, DateTime fiscalStartDate, FiscalYearSettings.Template template)
        {
            int periodsPerYear;
            
            switch (template)
            {
                case FiscalYearSettings.Template.Annually:
                    periodsPerYear = 1;
                    break;
                case FiscalYearSettings.Template.SemiAnnually:
                    periodsPerYear = 2;
                    break;
                case FiscalYearSettings.Template.Quarterly:
                    periodsPerYear = 4;
                    break;
                case FiscalYearSettings.Template.Monthly:
                    periodsPerYear = 12;
                    break;
                case FiscalYearSettings.Template.FourWeek:
                    periodsPerYear = 13;
                    break;
                default:
                    periodsPerYear = 1;
                    break;
            }

            if (currentPeriod > 1)
            {
                return (currentYear, currentPeriod - 1);
            }
            else
            {
                return (currentYear - 1, periodsPerYear);
            }
        }

        /// <summary>
        /// Gets the next fiscal period and year
        /// </summary>
        private static (int year, int period) GetNextFiscalPeriod(int currentYear, int currentPeriod, DateTime fiscalStartDate, FiscalYearSettings.Template template)
        {
            int periodsPerYear;
            
            switch (template)
            {
                case FiscalYearSettings.Template.Annually:
                    periodsPerYear = 1;
                    break;
                case FiscalYearSettings.Template.SemiAnnually:
                    periodsPerYear = 2;
                    break;
                case FiscalYearSettings.Template.Quarterly:
                    periodsPerYear = 4;
                    break;
                case FiscalYearSettings.Template.Monthly:
                    periodsPerYear = 12;
                    break;
                case FiscalYearSettings.Template.FourWeek:
                    periodsPerYear = 13;
                    break;
                default:
                    periodsPerYear = 1;
                    break;
            }

            if (currentPeriod < periodsPerYear)
            {
                return (currentYear, currentPeriod + 1);
            }
            else
            {
                return (currentYear + 1, 1);
            }
        }
    }
}
