using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents a Parallel Branch action in a Cloud Flow.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/use-parallel-branches
    /// 
    /// Parallel Branch actions execute multiple branches of actions simultaneously (or in simulation, sequentially).
    /// Each branch is independent and can contain its own sequence of actions.
    /// All branches complete before the flow continues to subsequent actions.
    /// 
    /// Common use cases:
    /// - Execute multiple independent actions simultaneously
    /// - Send notifications to multiple channels at once
    /// - Perform parallel data retrieval or processing
    /// - Optimize flow execution time
    /// 
    /// Note: In testing simulation, branches execute sequentially but are logically independent.
    /// </summary>
    public class ParallelBranchAction : IFlowAction
    {
        public ParallelBranchAction()
        {
            ActionType = "ParallelBranch";
            Parameters = new Dictionary<string, object>();
            Branches = new List<ParallelBranch>();
        }

        /// <summary>
        /// Gets or sets the action type. Always "ParallelBranch" for this action.
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the action name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the parallel branches to execute.
        /// Each branch contains its own list of actions.
        /// </summary>
        public IList<ParallelBranch> Branches { get; set; }

        /// <summary>
        /// Gets or sets action parameters (implements IFlowAction.Parameters)
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }
    }

    /// <summary>
    /// Represents a single branch in a parallel execution.
    /// </summary>
    public class ParallelBranch
    {
        public ParallelBranch()
        {
            Actions = new List<IFlowAction>();
        }

        /// <summary>
        /// Gets or sets the name of this branch.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the actions to execute in this branch.
        /// </summary>
        public IList<IFlowAction> Actions { get; set; }
    }
}
