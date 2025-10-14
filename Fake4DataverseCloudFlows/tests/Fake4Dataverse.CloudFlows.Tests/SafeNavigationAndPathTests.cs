#if !NET462
using System;
using System.Collections.Generic;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.CloudFlows;
using Fake4Dataverse.CloudFlows.Expressions;
using Xunit;

namespace Fake4Dataverse.Tests.CloudFlows
{
    /// <summary>
    /// Tests for safe navigation operator (?) and path separator (/) support in expressions.
    /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference
    /// 
    /// These features are essential for real-world Power Automate flows.
    /// </summary>
    public class SafeNavigationAndPathTests
    {
        #region Safe Navigation Operator Tests
        
        [Fact]
        public void Should_Support_Safe_Navigation_With_Null_Value()
        {
            // Reference: Power Automate uses ?['field'] for null-safe property access
            // When the object is null, the expression should return null instead of throwing
            // Arrange
            var triggerInputs = new Dictionary<string, object>
            {
                ["contact"] = null
            };
            var context = new FlowExecutionContext(triggerInputs);
            var evaluator = new ExpressionEvaluator(context);

            // Act
            var result = evaluator.Evaluate("@triggerBody()['contact']?['firstname']");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Should_Support_Safe_Navigation_With_Valid_Value()
        {
            // Reference: Power Automate uses ?['field'] for null-safe property access
            // When the object exists, it should access the property normally
            // Arrange
            var contact = new Dictionary<string, object>
            {
                ["firstname"] = "John"
            };
            var triggerInputs = new Dictionary<string, object>
            {
                ["contact"] = contact
            };
            var context = new FlowExecutionContext(triggerInputs);
            var evaluator = new ExpressionEvaluator(context);

            // Act
            var result = evaluator.Evaluate("@triggerBody()['contact']?['firstname']");

            // Assert
            Assert.Equal("John", result);
        }

        [Fact]
        public void Should_Support_Chained_Safe_Navigation()
        {
            // Reference: Multiple ?[] operators can be chained for deep null-safe access
            // Arrange
            var triggerInputs = new Dictionary<string, object>
            {
                ["account"] = null
            };
            var context = new FlowExecutionContext(triggerInputs);
            var evaluator = new ExpressionEvaluator(context);

            // Act
            var result = evaluator.Evaluate("@triggerBody()?['account']?['contact']?['firstname']");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Should_Support_Safe_Navigation_With_Missing_Field()
        {
            // Reference: ?['field'] should return null if field doesn't exist
            // Arrange
            var triggerInputs = new Dictionary<string, object>
            {
                ["firstname"] = "John"
                // middlename is missing
            };
            var context = new FlowExecutionContext(triggerInputs);
            var evaluator = new ExpressionEvaluator(context);

            // Act
            var result = evaluator.Evaluate("@triggerBody()?['middlename']");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Path Separator Tests

        [Fact]
        public void Should_Support_Path_Separator_In_Property_Access()
        {
            // Reference: Power Automate uses /path/separator for nested property access
            // Example: outputs('ActionName')?['body/fieldname'] to access body.fieldname
            // Arrange
            var actionOutputs = new Dictionary<string, object>
            {
                ["body"] = new Dictionary<string, object>
                {
                    ["firstname"] = "Jane"
                }
            };
            var context = new FlowExecutionContext(new Dictionary<string, object>());
            AddActionOutputs(context, "Get_Contact", actionOutputs);
            var evaluator = new ExpressionEvaluator(context);

            // Act
            var result = evaluator.Evaluate("@outputs('Get_Contact')['body/firstname']");

            // Assert
            Assert.Equal("Jane", result);
        }

        [Fact]
        public void Should_Support_Deep_Path_Separator()
        {
            // Reference: Path separators can be used for deep property access
            // Arrange
            var actionOutputs = new Dictionary<string, object>
            {
                ["body"] = new Dictionary<string, object>
                {
                    ["contact"] = new Dictionary<string, object>
                    {
                        ["address"] = new Dictionary<string, object>
                        {
                            ["city"] = "Seattle"
                        }
                    }
                }
            };
            var context = new FlowExecutionContext(new Dictionary<string, object>());
            AddActionOutputs(context, "Get_Data", actionOutputs);
            var evaluator = new ExpressionEvaluator(context);

            // Act
            var result = evaluator.Evaluate("@outputs('Get_Data')['body/contact/address/city']");

            // Assert
            Assert.Equal("Seattle", result);
        }

        [Fact]
        public void Should_Support_Path_Separator_With_Safe_Navigation()
        {
            // Reference: Combine path separator with safe navigation for robust access
            // Arrange
            var actionOutputs = new Dictionary<string, object>
            {
                ["body"] = null
            };
            var context = new FlowExecutionContext(new Dictionary<string, object>());
            AddActionOutputs(context, "Get_Contact", actionOutputs);
            var evaluator = new ExpressionEvaluator(context);

            // Act
            var result = evaluator.Evaluate("@outputs('Get_Contact')?['body/firstname']");

            // Assert
            Assert.Null(result);
        }

        #endregion

        /// <summary>
        /// Helper method to add action outputs using reflection
        /// </summary>
        private void AddActionOutputs(IFlowExecutionContext context, string actionName, IDictionary<string, object> outputs)
        {
            var method = context.GetType().GetMethod("AddActionOutputs", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method.Invoke(context, new object[] { actionName, outputs });
        }
    }
}
#endif
