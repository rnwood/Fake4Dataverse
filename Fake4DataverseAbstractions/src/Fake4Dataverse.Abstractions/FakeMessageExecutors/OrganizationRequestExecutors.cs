using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.FakeMessageExecutors
{
    /// <summary>
    /// Holds executors that handle OrganizationRequest type based on CanExecute() method.
    /// Used for executors like Custom API and NavigateToNextEntity that need dynamic routing.
    /// </summary>
    public class OrganizationRequestExecutors : List<IFakeMessageExecutor>
    {
        public OrganizationRequestExecutors() : base()
        {
        }

        public OrganizationRequestExecutors(IEnumerable<IFakeMessageExecutor> executors) : base(executors)
        {
        }
    }
}
