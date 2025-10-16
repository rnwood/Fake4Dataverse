
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace Fake4Dataverse.Abstractions.Settings
{

    [EntityLogicalName("organization")]
    public class FiscalYearSettings
    {
        [AttributeLogicalName("fiscalcalendarstart")]
        public DateTime StartDate { get; set; }

        [AttributeLogicalName("fiscalperiodtype")]
        public Template FiscalPeriodTemplate { get; set; }

        public enum Template
        {
            Annually = 2000,
            SemiAnnually = 2001,
            Quarterly = 2002,
            Monthly = 2003,
            FourWeek = 2004
        }

        public FiscalYearSettings()
        {
            FiscalPeriodTemplate = Template.Annually;
            StartDate = new DateTime(DateTime.UtcNow.Year, 1, 1);
        }
    }
}