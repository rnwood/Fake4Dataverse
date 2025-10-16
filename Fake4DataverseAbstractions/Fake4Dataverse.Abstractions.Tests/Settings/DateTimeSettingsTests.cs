using Fake4Dataverse.Abstractions.Settings;
using Xunit;

namespace Fake4Dataverse.Abstractions.Tests.Settings
{
    public class DateTimeSettingsTests
    {
        [Fact]
        public void Should_create_date_time_settings()
        {
            var dateTimeSettings = new DateTimeSettings();
            Assert.Null(dateTimeSettings.SystemTimeZone);
        }
    }
}
