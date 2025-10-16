
using System;
using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.FakeMessageExecutors
{
    public class MessageExecutors : Dictionary<Type, IFakeMessageExecutor>
    {
        public MessageExecutors(Dictionary<Type, IFakeMessageExecutor> other): base(other)
        {
            
        }
    }
}