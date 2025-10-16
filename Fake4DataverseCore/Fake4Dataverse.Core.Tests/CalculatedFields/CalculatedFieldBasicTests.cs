using Fake4Dataverse.CalculatedFields;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace Fake4Dataverse.Tests.CalculatedFields
{
    /// <summary>
    /// Tests for basic calculated field evaluation with arithmetic operations.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
    /// "Define calculated columns - Create columns that automatically calculate their values based on other column values"
    /// </summary>
    public class CalculatedFieldBasicTests : Fake4DataverseTests
    {
        [Fact]
        public void Should_Evaluate_Simple_Arithmetic_Formula()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
            // Basic arithmetic operators: +, -, *, / are supported in calculated fields
            var context = new XrmFakedContext();
            var evaluator = context.CalculatedFieldEvaluator;

            var definition = new CalculatedFieldDefinition
            {
                EntityLogicalName = "product",
                AttributeLogicalName = "totalprice",
                Formula = "[quantity] * [unitprice]",
                ResultType = typeof(decimal),
                Dependencies = { "quantity", "unitprice" }
            };
            evaluator.RegisterCalculatedField(definition);

            var product = new Entity("product")
            {
                Id = Guid.NewGuid(),
                ["quantity"] = 10,
                ["unitprice"] = 25.50m
            };

            // Act
            evaluator.EvaluateCalculatedFields(product);

            // Assert
            Assert.True(product.Contains("totalprice"));
            Assert.Equal(255.00m, product.GetAttributeValue<decimal>("totalprice"));
        }

        [Fact]
        public void Should_Evaluate_CONCAT_Function()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
            // CONCAT function: "Combines multiple text values into a single text value"
            var context = new XrmFakedContext();
            var evaluator = context.CalculatedFieldEvaluator;

            var definition = new CalculatedFieldDefinition
            {
                EntityLogicalName = "contact",
                AttributeLogicalName = "fullname",
                Formula = "CONCAT([firstname], ' ', [lastname])",
                ResultType = typeof(string)
            };
            evaluator.RegisterCalculatedField(definition);

            var contact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["lastname"] = "Doe"
            };

            // Act
            evaluator.EvaluateCalculatedFields(contact);

            // Assert
            Assert.Equal("John Doe", contact.GetAttributeValue<string>("fullname"));
        }

        [Fact]
        public void Should_Evaluate_DIFFINDAYS_Function()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
            // DIFFINDAYS function: "Returns the difference between two dates in days"
            var context = new XrmFakedContext();
            var evaluator = context.CalculatedFieldEvaluator;

            var definition = new CalculatedFieldDefinition
            {
                EntityLogicalName = "task",
                AttributeLogicalName = "duration",
                Formula = "DIFFINDAYS([startdate], [enddate])",
                ResultType = typeof(int)
            };
            evaluator.RegisterCalculatedField(definition);

            var task = new Entity("task")
            {
                Id = Guid.NewGuid(),
                ["startdate"] = new DateTime(2025, 1, 1),
                ["enddate"] = new DateTime(2025, 1, 11)
            };

            // Act
            evaluator.EvaluateCalculatedFields(task);

            // Assert
            Assert.Equal(10, task.GetAttributeValue<int>("duration"));
        }

        [Fact]
        public void Should_Evaluate_ADDDAYS_Function()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
            // ADDDAYS function: "Adds specified days to a date"
            var context = new XrmFakedContext();
            var evaluator = context.CalculatedFieldEvaluator;

            var definition = new CalculatedFieldDefinition
            {
                EntityLogicalName = "task",
                AttributeLogicalName = "duedate",
                Formula = "ADDDAYS([createdon], 7)",
                ResultType = typeof(DateTime)
            };
            evaluator.RegisterCalculatedField(definition);

            var task = new Entity("task")
            {
                Id = Guid.NewGuid(),
                ["createdon"] = new DateTime(2025, 1, 1)
            };

            // Act
            evaluator.EvaluateCalculatedFields(task);

            // Assert
            Assert.Equal(new DateTime(2025, 1, 8), task.GetAttributeValue<DateTime>("duedate"));
        }

        [Fact]
        public void Should_Evaluate_TRIMLEFT_Function()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
            // TRIMLEFT function: "Removes leading whitespace from a text value"
            var context = new XrmFakedContext();
            var evaluator = context.CalculatedFieldEvaluator;

            var definition = new CalculatedFieldDefinition
            {
                EntityLogicalName = "entity",
                AttributeLogicalName = "cleanname",
                Formula = "TRIMLEFT([rawname])",
                ResultType = typeof(string)
            };
            evaluator.RegisterCalculatedField(definition);

            var entity = new Entity("entity")
            {
                Id = Guid.NewGuid(),
                ["rawname"] = "   Test Name"
            };

            // Act
            evaluator.EvaluateCalculatedFields(entity);

            // Assert
            Assert.Equal("Test Name", entity.GetAttributeValue<string>("cleanname"));
        }

        [Fact]
        public void Should_Evaluate_IF_Function()
        {
            // Arrange
            var context = new XrmFakedContext();
            var evaluator = context.CalculatedFieldEvaluator;

            var definition = new CalculatedFieldDefinition
            {
                EntityLogicalName = "student",
                AttributeLogicalName = "grade",
                Formula = "IF([score] >= 60, 'Pass', 'Fail')",
                ResultType = typeof(string)
            };
            evaluator.RegisterCalculatedField(definition);

            var passingStudent = new Entity("student")
            {
                Id = Guid.NewGuid(),
                ["score"] = 75
            };

            var failingStudent = new Entity("student")
            {
                Id = Guid.NewGuid(),
                ["score"] = 45
            };

            // Act
            evaluator.EvaluateCalculatedFields(passingStudent);
            evaluator.EvaluateCalculatedFields(failingStudent);

            // Assert
            Assert.Equal("Pass", passingStudent.GetAttributeValue<string>("grade"));
            Assert.Equal("Fail", failingStudent.GetAttributeValue<string>("grade"));
        }

        [Fact]
        public void Should_Evaluate_Logical_AND_Operator()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
            // "The logical operations include AND and OR operators"
            var context = new XrmFakedContext();
            var evaluator = context.CalculatedFieldEvaluator;

            var definition = new CalculatedFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "isqualified",
                Formula = "[isactive] AND [hasrevenue]",
                ResultType = typeof(bool)
            };
            evaluator.RegisterCalculatedField(definition);

            var qualifiedAccount = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["isactive"] = true,
                ["hasrevenue"] = true
            };

            // Act
            evaluator.EvaluateCalculatedFields(qualifiedAccount);

            // Assert
            Assert.True(qualifiedAccount.GetAttributeValue<bool>("isqualified"));
        }

        [Fact]
        public void Should_Evaluate_Calculated_Field_On_Retrieve()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
            // "Calculated columns are calculated in real-time when they are retrieved"
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.CalculatedFieldEvaluator;

            var definition = new CalculatedFieldDefinition
            {
                EntityLogicalName = "product",
                AttributeLogicalName = "totalprice",
                Formula = "[quantity] * [unitprice]",
                ResultType = typeof(decimal)
            };
            evaluator.RegisterCalculatedField(definition);

            var product = new Entity("product")
            {
                Id = Guid.NewGuid(),
                ["quantity"] = 5,
                ["unitprice"] = 10.00m
            };

            context.Initialize(new[] { product });
            var service = _service;

            // Act
            var retrieved = service.Retrieve("product", product.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));

            // Assert
            Assert.True(retrieved.Contains("totalprice"));
            Assert.Equal(50.00m, retrieved.GetAttributeValue<decimal>("totalprice"));
        }
    }
}
