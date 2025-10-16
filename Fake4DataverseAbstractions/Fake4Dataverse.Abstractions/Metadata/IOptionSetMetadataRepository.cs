using System.Linq;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Abstractions.Metadata
{
    public interface IOptionSetMetadataRepository
    {
        OptionSetMetadata GetByName(string sGlobalOptionSetName);
        void Set(string sGlobalOptionSetName, OptionSetMetadata metadata);

        IQueryable<OptionSetMetadata> CreateQuery();
    }
    
}