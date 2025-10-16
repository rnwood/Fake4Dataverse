
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Abstractions
{
    public interface IXrmFakedTracingService: ITracingService
    {
        string DumpTrace();
    }
}