using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Abstractions
{
    public interface ICallerProperties
    {
        EntityReference CallerId { get; set; }
        EntityReference BusinessUnitId { get; set; }
    }
}
