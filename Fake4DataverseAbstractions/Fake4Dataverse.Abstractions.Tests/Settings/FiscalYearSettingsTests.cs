using System;
using Fake4Dataverse.Abstractions.Settings;
using Xunit;

namespace Fake4Dataverse.Abstractions.Tests.Settings
{
    public class FiscalYearSettingsTests
    {
        [Fact]
        public void Should_create_fiscal_year_settings_with_defaults()
        {
            var fiscalYearSettings = new FiscalYearSettings();
            
            Assert.Equal(FiscalYearSettings.Template.Annually, fiscalYearSettings.FiscalPeriodTemplate);
            Assert.Equal(DateTime.UtcNow.Year, fiscalYearSettings.StartDate.Year);
            Assert.Equal(1, fiscalYearSettings.StartDate.Month);
            Assert.Equal(1, fiscalYearSettings.StartDate.Day);
        }
    }
}
