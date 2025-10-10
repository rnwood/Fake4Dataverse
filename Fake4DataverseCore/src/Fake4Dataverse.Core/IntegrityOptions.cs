using Fake4Dataverse.Abstractions.Integrity;

namespace Fake4Dataverse.Integrity
{
    public class IntegrityOptions : IIntegrityOptions
    {
        public bool ValidateEntityReferences { get; set; }

        public IntegrityOptions()
        {
            ValidateEntityReferences = true;
        }
    }
}
