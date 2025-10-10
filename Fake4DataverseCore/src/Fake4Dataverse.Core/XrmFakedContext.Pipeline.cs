using Fake4Dataverse.Abstractions;

namespace Fake4Dataverse
{
    public partial class XrmFakedContext : IXrmFakedContext
    {
        public bool UsePipelineSimulation { get; set; }

        
    }
}