
using System;
using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.FakeMessageExecutors
{
    public class ExecutionMocks : Dictionary<Type, OrganizationRequestExecution>
    {
        public ExecutionMocks() { }
    }
}