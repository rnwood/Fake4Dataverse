using System;
using Xunit;

namespace Fake4Dataverse.Service.IntegrationTests
{
    /// <summary>
    /// Skip tests when running on .NET Framework 4.6.2 (net462).
    /// The Fake4Dataverse.Service project only targets .NET 8.0 and cannot run on net462.
    /// </summary>
    public sealed class SkipOnNet462Attribute : FactAttribute
    {
        public SkipOnNet462Attribute()
        {
#if NET462
            Skip = "Service integration tests are not supported on .NET Framework 4.6.2. Requires .NET 8.0+";
#endif
        }
    }
}
