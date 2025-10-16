using Fake4Dataverse.Abstractions;
using System;

namespace Fake4Dataverse
{
    public partial class XrmFakedContext : IXrmFakedContext
    {
        public TimeZoneInfo SystemTimeZone { get; set; }
    }
}