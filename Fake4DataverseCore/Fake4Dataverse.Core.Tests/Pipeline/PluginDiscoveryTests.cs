using Xunit;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fake4Dataverse.Tests.Pipeline
{
    /// <summary>
    /// Tests for plugin auto-discovery functionality
    /// </summary>
    public class PluginDiscoveryTests : Fake4DataverseTests
    {
        [Fact]
        public void Should_DiscoverPlugins_WithSPKLAttributes()
        {
            // Arrange
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var registrations = PluginDiscoveryService.DiscoverPlugins(assemblies).ToList();

            // Assert - Should find plugins with CrmPluginRegistrationAttribute
            Assert.NotEmpty(registrations);
            var testPluginRegistrations = registrations.Where(r => r.PluginType == typeof(TestPluginWithSPKLAttribute)).ToList();
            Assert.NotEmpty(testPluginRegistrations);
        }

        [Fact]
        public void Should_RegisterDiscoveredPlugins_InPipelineSimulator()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var count = context.PluginPipelineSimulator.DiscoverAndRegisterPlugins(assemblies);

            // Assert
            Assert.True(count > 0);
        }

        [Fact]
        public void Should_UseCustomConverter_WhenProvided()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var assemblies = new[] { Assembly.GetExecutingAssembly() };
            
            Func<Type, IEnumerable<PluginStepRegistration>> customConverter = (pluginType) =>
            {
                if (pluginType == typeof(TestPluginWithoutAttributes))
                {
                    return new[]
                    {
                        new PluginStepRegistration
                        {
                            PluginType = pluginType,
                            MessageName = "Create",
                            PrimaryEntityName = "contact",
                            Stage = ProcessingStepStage.Preoperation
                        }
                    };
                }
                return Enumerable.Empty<PluginStepRegistration>();
            };

            // Act
            var count = context.PluginPipelineSimulator.DiscoverAndRegisterPlugins(assemblies, customConverter);

            // Assert
            Assert.True(count > 0);
            var registeredSteps = context.PluginPipelineSimulator.GetRegisteredPluginSteps(
                "Create", "contact", ProcessingStepStage.Preoperation).ToList();
            Assert.Contains(registeredSteps, r => r.PluginType == typeof(TestPluginWithoutAttributes));
        }

        [Fact]
        public void Should_UseAttributeConverter_WhenProvided()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            Func<Type, Attribute, PluginStepRegistration> attributeConverter = (pluginType, attribute) =>
            {
                if (attribute is TestCustomAttribute customAttr)
                {
                    return new PluginStepRegistration
                    {
                        PluginType = pluginType,
                        MessageName = customAttr.Message,
                        PrimaryEntityName = customAttr.Entity,
                        Stage = ProcessingStepStage.Preoperation
                    };
                }
                return null;
            };

            // Act
            var count = context.PluginPipelineSimulator.DiscoverAndRegisterPluginsWithAttributeConverter(
                assemblies,
                typeof(TestCustomAttribute),
                attributeConverter);

            // Assert
            Assert.True(count > 0);
            var registeredSteps = context.PluginPipelineSimulator.GetRegisteredPluginSteps(
                "Update", "account", ProcessingStepStage.Preoperation).ToList();
            Assert.Contains(registeredSteps, r => r.PluginType == typeof(TestPluginWithCustomAttribute));
        }

        [Fact]
        public void Should_AutoExecuteDiscoveredPlugins_WhenUsePipelineSimulation()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            context.UsePipelineSimulation = true;
            
            var assemblies = new[] { Assembly.GetExecutingAssembly() };
            
            Func<Type, IEnumerable<PluginStepRegistration>> customConverter = (pluginType) =>
            {
                if (pluginType == typeof(TestDiscoveredPlugin))
                {
                    return new[]
                    {
                        new PluginStepRegistration
                        {
                            PluginType = pluginType,
                            MessageName = "Create",
                            PrimaryEntityName = "account",
                            Stage = ProcessingStepStage.Preoperation
                        }
                    };
                }
                return Enumerable.Empty<PluginStepRegistration>();
            };

            TestDiscoveredPlugin.WasExecuted = false;
            context.PluginPipelineSimulator.DiscoverAndRegisterPlugins(assemblies, customConverter);

            var service = _service;
            var account = new Entity("account") { ["name"] = "Test" };

            // Act
            service.Create(account);

            // Assert
            Assert.True(TestDiscoveredPlugin.WasExecuted);
        }

        [Fact]
        public void Should_DiscoverPreImageFromSPKLAttribute()
        {
            // Arrange
            // Reference: https://github.com/scottdurow/SparkleXrm/wiki/spkl#image-registration
            // SPKL CrmPluginRegistrationImageAttribute defines pre/post images for plugin steps
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var registrations = PluginDiscoveryService.DiscoverPlugins(assemblies).ToList();

            // Assert - Should find plugin with pre-image
            var pluginWithPreImage = registrations
                .FirstOrDefault(r => r.PluginType == typeof(TestPluginWithPreImage));
            
            Assert.NotNull(pluginWithPreImage);
            Assert.NotEmpty(pluginWithPreImage.PreImages);
            
            var preImage = pluginWithPreImage.PreImages.First();
            Assert.Equal("PreImage", preImage.Name);
            Assert.Equal(ProcessingStepImageType.PreImage, preImage.ImageType);
            Assert.Contains("name", preImage.Attributes);
            Assert.Contains("revenue", preImage.Attributes);
            Assert.Equal(2, preImage.Attributes.Count);
        }

        [Fact]
        public void Should_DiscoverPostImageFromSPKLAttribute()
        {
            // Arrange
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var registrations = PluginDiscoveryService.DiscoverPlugins(assemblies).ToList();

            // Assert - Should find plugin with post-image
            var pluginWithPostImage = registrations
                .FirstOrDefault(r => r.PluginType == typeof(TestPluginWithPostImage));
            
            Assert.NotNull(pluginWithPostImage);
            Assert.NotEmpty(pluginWithPostImage.PostImages);
            
            var postImage = pluginWithPostImage.PostImages.First();
            Assert.Equal("PostImage", postImage.Name);
            Assert.Equal(ProcessingStepImageType.PostImage, postImage.ImageType);
            Assert.Empty(postImage.Attributes); // Empty attributes = all attributes
        }

        [Fact]
        public void Should_DiscoverMultipleImagesFromSPKLAttributes()
        {
            // Arrange
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var registrations = PluginDiscoveryService.DiscoverPlugins(assemblies).ToList();

            // Assert - Should find plugin with both pre and post images
            var pluginWithMultipleImages = registrations
                .FirstOrDefault(r => r.PluginType == typeof(TestPluginWithMultipleImages));
            
            Assert.NotNull(pluginWithMultipleImages);
            Assert.NotEmpty(pluginWithMultipleImages.PreImages);
            Assert.NotEmpty(pluginWithMultipleImages.PostImages);
            
            var preImage = pluginWithMultipleImages.PreImages.First();
            Assert.Equal("PreImage", preImage.Name);
            Assert.Contains("firstname", preImage.Attributes);
            Assert.Contains("lastname", preImage.Attributes);
            
            var postImage = pluginWithMultipleImages.PostImages.First();
            Assert.Equal("PostImage", postImage.Name);
        }

        [Fact]
        public void Should_RegisterPluginWithImages_InPipelineSimulator()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var count = context.PluginPipelineSimulator.DiscoverAndRegisterPlugins(assemblies);

            // Assert - Verify plugins with images are registered
            var registeredSteps = context.PluginPipelineSimulator.GetRegisteredPluginSteps(
                "Update", "account", ProcessingStepStage.Preoperation).ToList();
            
            var pluginWithImages = registeredSteps
                .FirstOrDefault(r => r.PluginType == typeof(TestPluginWithPreImage));
            
            Assert.NotNull(pluginWithImages);
            Assert.NotEmpty(pluginWithImages.PreImages);
        }

        [Fact]
        public void Should_DiscoverPlugins_WithXrmToolsMetaAttributes()
        {
            // Arrange
            // Reference: https://www.nuget.org/packages/XrmTools.Meta/
            // XrmTools.Meta StepAttribute defines plugin step registration
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var registrations = PluginDiscoveryService.DiscoverPlugins(assemblies).ToList();

            // Assert - Should find plugins with XrmTools.Meta StepAttribute
            Assert.NotEmpty(registrations);
            var testPluginRegistrations = registrations
                .Where(r => r.PluginType == typeof(TestPluginWithXrmToolsStepAttribute))
                .ToList();
            Assert.NotEmpty(testPluginRegistrations);
            
            var registration = testPluginRegistrations.First();
            Assert.Equal("Create", registration.MessageName);
            Assert.Equal("account", registration.PrimaryEntityName);
            Assert.Equal(ProcessingStepStage.Preoperation, registration.Stage);
            Assert.Equal(ProcessingStepMode.Synchronous, registration.Mode);
        }

        [Fact]
        public void Should_DiscoverPreImage_FromXrmToolsMetaAttribute()
        {
            // Arrange
            // Reference: https://www.nuget.org/packages/XrmTools.Meta/
            // XrmTools.Meta ImageAttribute defines pre/post images for plugin steps
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var registrations = PluginDiscoveryService.DiscoverPlugins(assemblies).ToList();

            // Assert - Should find plugin with pre-image
            var pluginWithPreImage = registrations
                .FirstOrDefault(r => r.PluginType == typeof(TestPluginWithXrmToolsPreImage));
            
            Assert.NotNull(pluginWithPreImage);
            Assert.NotEmpty(pluginWithPreImage.PreImages);
            
            var preImage = pluginWithPreImage.PreImages.First();
            Assert.Equal("PreImage", preImage.Name);
            Assert.Equal(ProcessingStepImageType.PreImage, preImage.ImageType);
            Assert.Contains("firstname", preImage.Attributes);
            Assert.Contains("lastname", preImage.Attributes);
            Assert.Contains("emailaddress", preImage.Attributes);
            Assert.Equal(3, preImage.Attributes.Count);
        }

        [Fact]
        public void Should_DiscoverPostImage_FromXrmToolsMetaAttribute()
        {
            // Arrange
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var registrations = PluginDiscoveryService.DiscoverPlugins(assemblies).ToList();

            // Assert - Should find plugin with post-image
            var pluginWithPostImage = registrations
                .FirstOrDefault(r => r.PluginType == typeof(TestPluginWithXrmToolsPostImage));
            
            Assert.NotNull(pluginWithPostImage);
            Assert.NotEmpty(pluginWithPostImage.PostImages);
            
            var postImage = pluginWithPostImage.PostImages.First();
            Assert.Equal("PostImage", postImage.Name);
            Assert.Equal(ProcessingStepImageType.PostImage, postImage.ImageType);
            Assert.Empty(postImage.Attributes); // Empty attributes = all attributes
        }

        [Fact]
        public void Should_DiscoverMultipleImages_FromXrmToolsMetaAttributes()
        {
            // Arrange
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var registrations = PluginDiscoveryService.DiscoverPlugins(assemblies).ToList();

            // Assert - Should find plugin with both pre and post images
            var pluginWithMultipleImages = registrations
                .FirstOrDefault(r => r.PluginType == typeof(TestPluginWithXrmToolsMultipleImages));
            
            Assert.NotNull(pluginWithMultipleImages);
            Assert.NotEmpty(pluginWithMultipleImages.PreImages);
            Assert.NotEmpty(pluginWithMultipleImages.PostImages);
            
            var preImage = pluginWithMultipleImages.PreImages.First();
            Assert.Equal("PreImage", preImage.Name);
            Assert.Contains("subject", preImage.Attributes);
            Assert.Contains("description", preImage.Attributes);
            Assert.Contains("statuscode", preImage.Attributes);
            
            var postImage = pluginWithMultipleImages.PostImages.First();
            Assert.Equal("PostImage", postImage.Name);
            Assert.Contains("subject", postImage.Attributes);
            Assert.Contains("description", postImage.Attributes);
            Assert.Contains("statuscode", postImage.Attributes);
        }

        [Fact]
        public void Should_DiscoverFilteringAttributes_FromXrmToolsMetaAttribute()
        {
            // Arrange
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            // Act
            var registrations = PluginDiscoveryService.DiscoverPlugins(assemblies).ToList();

            // Assert - Should parse filtering attributes from XrmTools.Meta StepAttribute
            var pluginWithFiltering = registrations
                .FirstOrDefault(r => r.PluginType == typeof(TestPluginWithXrmToolsPreImage));
            
            Assert.NotNull(pluginWithFiltering);
            Assert.NotNull(pluginWithFiltering.FilteringAttributes);
            Assert.Contains("firstname", pluginWithFiltering.FilteringAttributes);
            Assert.Contains("lastname", pluginWithFiltering.FilteringAttributes);
            Assert.Equal(2, pluginWithFiltering.FilteringAttributes.Count);
        }
    }

    #region Test Plugins

    /// <summary>
    /// Test plugin with SPKL-style attribute (simulated using a custom attribute)
    /// </summary>
    [CrmPluginRegistration("Create", "account", ProcessingStepStage.Preoperation, 1)]
    public class TestPluginWithSPKLAttribute : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    /// <summary>
    /// Test plugin without any attributes
    /// </summary>
    public class TestPluginWithoutAttributes : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    /// <summary>
    /// Test plugin with custom attribute
    /// </summary>
    [TestCustomAttribute("Update", "account")]
    public class TestPluginWithCustomAttribute : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    /// <summary>
    /// Test plugin for auto-execution verification
    /// </summary>
    public class TestDiscoveredPlugin : IPlugin
    {
        public static bool WasExecuted { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            WasExecuted = true;
        }
    }

    /// <summary>
    /// Test plugin with SPKL image attributes for pre-image
    /// </summary>
    [CrmPluginRegistration("Update", "account", ProcessingStepStage.Preoperation, 1)]
    [CrmPluginRegistrationImage(ProcessingStepImageType.PreImage, "PreImage", "name,revenue")]
    public class TestPluginWithPreImage : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    /// <summary>
    /// Test plugin with SPKL image attributes for post-image
    /// </summary>
    [CrmPluginRegistration("Update", "contact", ProcessingStepStage.Postoperation, 1)]
    [CrmPluginRegistrationImage(ProcessingStepImageType.PostImage, "PostImage", "")]
    public class TestPluginWithPostImage : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    /// <summary>
    /// Test plugin with multiple SPKL image attributes
    /// </summary>
    [CrmPluginRegistration("Update", "lead", ProcessingStepStage.Preoperation, 1)]
    [CrmPluginRegistrationImage(ProcessingStepImageType.PreImage, "PreImage", "firstname,lastname")]
    [CrmPluginRegistrationImage(ProcessingStepImageType.PostImage, "PostImage", "")]
    public class TestPluginWithMultipleImages : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    #endregion

    #region Test Attributes

    /// <summary>
    /// Simulates SPKL CrmPluginRegistrationAttribute for testing
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CrmPluginRegistrationAttribute : Attribute
    {
        public string MessageName { get; set; }
        public string EntityLogicalName { get; set; }
        public ProcessingStepStage Stage { get; set; }
        public int ExecutionOrder { get; set; }
        public string FilteringAttributes { get; set; }

        public CrmPluginRegistrationAttribute(
            string messageName,
            string entityLogicalName,
            ProcessingStepStage stage,
            int executionOrder)
        {
            MessageName = messageName;
            EntityLogicalName = entityLogicalName;
            Stage = stage;
            ExecutionOrder = executionOrder;
        }
    }

    /// <summary>
    /// Custom attribute for testing custom attribute converter
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TestCustomAttribute : Attribute
    {
        public string Message { get; }
        public string Entity { get; }

        public TestCustomAttribute(string message, string entity)
        {
            Message = message;
            Entity = entity;
        }
    }

    /// <summary>
    /// Simulates SPKL CrmPluginRegistrationImageAttribute for testing
    /// Reference: https://github.com/scottdurow/SparkleXrm/wiki/spkl#image-registration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CrmPluginRegistrationImageAttribute : Attribute
    {
        public string MessageName { get; set; }
        public string EntityLogicalName { get; set; }
        public ProcessingStepImageType ImageType { get; set; }
        public string Name { get; set; }
        public string EntityAlias { get; set; }
        public string Attributes { get; set; }

        public CrmPluginRegistrationImageAttribute(
            ProcessingStepImageType imageType,
            string name,
            string attributes = "")
        {
            ImageType = imageType;
            Name = name;
            Attributes = attributes;
            EntityAlias = name; // Default to name
        }
    }

    #endregion
}

// Simulated XrmTools.Meta enums for testing
namespace Fake4Dataverse.Tests.Pipeline
{
    /// <summary>
    /// XrmTools.Meta Stages enum (matches XrmTools.Meta.Model.Stages)
    /// </summary>
    public enum XrmToolsStages
    {
        PreValidation = 10,
        PreOperation = 20,
        MainOperation = 30,
        PostOperation = 40,
        DepecratedPostOperation = 50
    }

    /// <summary>
    /// XrmTools.Meta ExecutionMode enum (matches XrmTools.Meta.Model.ExecutionMode)
    /// </summary>
    public enum XrmToolsExecutionMode
    {
        Synchronous = 0,
        Asynchronous = 1
    }

    /// <summary>
    /// XrmTools.Meta ImageTypes enum (matches XrmTools.Meta.Model.ImageTypes)
    /// </summary>
    public enum XrmToolsImageTypes
    {
        PreImage = 0,
        PostImage = 1,
        Both = 2
    }
}

// Simulated XrmTools.Meta.Attributes namespace for testing
namespace XrmTools.Meta.Attributes
{
    using System;
    using Fake4Dataverse.Tests.Pipeline;

    /// <summary>
    /// Simulates XrmTools.Meta StepAttribute for testing
    /// Reference: https://www.nuget.org/packages/XrmTools.Meta/
    /// Constructor: StepAttribute(string entityName, string message, string filteringAttributes, Stages stage, ExecutionMode mode)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class StepAttribute : Attribute
    {
        public string PrimaryEntityName { get; set; }
        public string MessageName { get; set; }
        public string FilteringAttributes { get; set; }
        public XrmToolsStages Stage { get; set; }
        public XrmToolsExecutionMode Mode { get; set; }
        public int ExecutionOrder { get; set; }

        public StepAttribute(
            string entityName,
            string message,
            string filteringAttributes,
            XrmToolsStages stage,
            XrmToolsExecutionMode mode)
        {
            PrimaryEntityName = entityName;
            MessageName = message;
            FilteringAttributes = filteringAttributes;
            Stage = stage;
            Mode = mode;
            ExecutionOrder = 1; // Default
        }
    }

    /// <summary>
    /// Simulates XrmTools.Meta ImageAttribute for testing
    /// Reference: https://www.nuget.org/packages/XrmTools.Meta/
    /// Constructor: ImageAttribute(ImageTypes type, string messagePropertyName, string attributes = "")
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ImageAttribute : Attribute
    {
        public XrmToolsImageTypes Type { get; set; }
        public string MessagePropertyName { get; set; }
        public string Name { get; set; }
        public string EntityAlias { get; set; }
        public string Attributes { get; set; }

        public ImageAttribute(
            XrmToolsImageTypes type,
            string messagePropertyName,
            string attributes = "")
        {
            Type = type;
            MessagePropertyName = messagePropertyName;
            Attributes = attributes;
            Name = messagePropertyName; // Default to message property name
            EntityAlias = messagePropertyName; // Default
        }
    }
}

namespace Fake4Dataverse.Tests.Pipeline
{
    using XrmTools.Meta.Attributes;

    #region Test Plugins with XrmTools.Meta Attributes

    /// <summary>
    /// Test plugin with XrmTools.Meta StepAttribute
    /// </summary>
    [Step("account", "Create", "", XrmToolsStages.PreOperation, XrmToolsExecutionMode.Synchronous)]
    public class TestPluginWithXrmToolsStepAttribute : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    /// <summary>
    /// Test plugin with XrmTools.Meta attributes including pre-image
    /// </summary>
    [Step("contact", "Update", "firstname,lastname", XrmToolsStages.PreOperation, XrmToolsExecutionMode.Synchronous)]
    [Image(XrmToolsImageTypes.PreImage, "PreImage", "firstname,lastname,emailaddress")]
    public class TestPluginWithXrmToolsPreImage : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    /// <summary>
    /// Test plugin with XrmTools.Meta attributes including post-image
    /// </summary>
    [Step("opportunity", "Update", "", XrmToolsStages.PostOperation, XrmToolsExecutionMode.Synchronous)]
    [Image(XrmToolsImageTypes.PostImage, "PostImage", "")]
    public class TestPluginWithXrmToolsPostImage : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    /// <summary>
    /// Test plugin with XrmTools.Meta attributes including both pre and post images
    /// </summary>
    [Step("lead", "Update", "subject,description", XrmToolsStages.PreOperation, XrmToolsExecutionMode.Synchronous)]
    [Image(XrmToolsImageTypes.PreImage, "PreImage", "subject,description,statuscode")]
    [Image(XrmToolsImageTypes.PostImage, "PostImage", "subject,description,statuscode")]
    public class TestPluginWithXrmToolsMultipleImages : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    #endregion
}
