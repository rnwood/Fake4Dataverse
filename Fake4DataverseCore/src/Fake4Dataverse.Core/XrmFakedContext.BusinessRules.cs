using Fake4Dataverse.BusinessRules;

namespace Fake4Dataverse
{
    /// <summary>
    /// Partial class containing business rules functionality.
    /// </summary>
    public partial class XrmFakedContext
    {
        private BusinessRuleExecutor _businessRuleExecutor;
        
        /// <summary>
        /// Gets the business rule executor for registering and executing business rules.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
        /// "Business rules provide a simple interface to implement and maintain fast-changing and commonly used rules.
        /// Business rules are defined visually and run on both the client and server."
        /// 
        /// Use this property to register business rules that should be executed during CRUD operations.
        /// Business rules are automatically executed during Create and Update operations when registered.
        /// </summary>
        public BusinessRuleExecutor BusinessRuleExecutor
        {
            get
            {
                if (_businessRuleExecutor == null)
                {
                    _businessRuleExecutor = new BusinessRuleExecutor();
                }
                return _businessRuleExecutor;
            }
        }
    }
}
