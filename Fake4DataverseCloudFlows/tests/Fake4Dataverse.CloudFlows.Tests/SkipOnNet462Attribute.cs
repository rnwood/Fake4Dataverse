using System;
using Xunit;

namespace Fake4Dataverse.Tests.CloudFlows
{
    /// <summary>
    /// Skip tests when running on .NET Framework 4.6.2 (net462).
    /// The Fake4Dataverse.CloudFlows project only targets .NET 8.0 and cannot run on net462.
    /// </summary>
    public sealed class SkipOnNet462Attribute : FactAttribute
    {
        public SkipOnNet462Attribute()
        {
#if NET462
            Skip = "Cloud Flows tests are not supported on .NET Framework 4.6.2. Requires .NET 8.0+";
#endif
        }
    }
}
