using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Xunit;

namespace Fake4Dataverse.Abstractions.Tests.FakeMessageExecutors
{
    public class ExecutionMocksTests
    {
        [Fact]
        public void Should_create_new_instance()
        {
            var executionMocks = new ExecutionMocks();
            Assert.NotNull(executionMocks);
        }
    }
}
