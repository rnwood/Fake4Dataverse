using Fake4Dataverse.Abstractions;
using Fake4Dataverse.CloudFlows;

namespace Fake4Dataverse.Tests.CloudFlows
{
    /// <summary>
    /// Extension methods for test setup
    /// </summary>
    public static class TestExtensions
    {
        /// <summary>
        /// Initializes CloudFlowSimulator for testing.
        /// Since CloudFlowSimulator is now in a separate package, it's not auto-initialized in Core.
        /// This extension method makes it easy to set up for tests.
        /// </summary>
        public static IXrmFakedContext WithCloudFlowSimulator(this IXrmFakedContext context)
        {
            context.CloudFlowSimulator = new CloudFlowSimulator(context);
            return context;
        }
    }
}
