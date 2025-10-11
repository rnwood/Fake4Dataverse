using Xunit;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using Crm;

namespace Fake4Dataverse.Tests.Pipeline
{
    /// <summary>
    /// Tests for plugin pre and post image support.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities
    /// 
    /// Entity images provide snapshots of entity data at different points in the pipeline:
    /// - Pre-images: Entity state before the core operation (Update, Delete)
    /// - Post-images: Entity state after the core operation (Create, Update)
    /// 
    /// Images can be filtered to include only specific attributes for performance.
    /// </summary>
    public class PluginImageTests
    {
        [Fact]
        public void Should_CreatePreImage_ForUpdateMessage_WithAllAttributes()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities
            // Pre-images are available for Update messages and capture the entity state before the update
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            // Create an existing account
            var accountId = Guid.NewGuid();
            var existingAccount = new Account
            {
                Id = accountId,
                Name = "Original Name",
                AccountNumber = "ACC-001",
                Telephone1 = "555-0100"
            };
            context.Initialize(existingAccount);

            // Register plugin with pre-image configuration
            ImageTestPlugin.ReceivedImages.Clear();
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Update",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(ImageTestPlugin),
                PreImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "PreImage",
                        EntityAlias = "PreImage",
                        ImageType = ProcessingStepImageType.PreImage,
                        Attributes = new HashSet<string>() // Empty = all attributes
                    }
                }
            });

            var service = context.GetOrganizationService();

            // Act - Update the account
            var accountUpdate = new Account
            {
                Id = accountId,
                Name = "Updated Name"
            };
            service.Update(accountUpdate);

            // Assert
            Assert.True(ImageTestPlugin.ReceivedImages.ContainsKey("PreImage"));
            var preImage = ImageTestPlugin.ReceivedImages["PreImage"];
            
            Assert.Equal("account", preImage.LogicalName);
            Assert.Equal(accountId, preImage.Id);
            Assert.Equal("Original Name", preImage.GetAttributeValue<string>("name"));
            Assert.Equal("ACC-001", preImage.GetAttributeValue<string>("accountnumber"));
            Assert.Equal("555-0100", preImage.GetAttributeValue<string>("telephone1"));
        }

        [Fact]
        public void Should_CreatePreImage_ForUpdateMessage_WithFilteredAttributes()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#filter-attributes
            // Images can be filtered to include only specific attributes for performance
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            var accountId = Guid.NewGuid();
            var existingAccount = new Account
            {
                Id = accountId,
                Name = "Original Name",
                AccountNumber = "ACC-001",
                Telephone1 = "555-0100",
                Revenue = new Money(100000)
            };
            context.Initialize(existingAccount);

            // Register plugin with filtered pre-image (only name and accountnumber)
            ImageTestPlugin.ReceivedImages.Clear();
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Update",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(ImageTestPlugin),
                PreImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "FilteredPreImage",
                        EntityAlias = "FilteredPreImage",
                        ImageType = ProcessingStepImageType.PreImage,
                        Attributes = new HashSet<string> { "name", "accountnumber" }
                    }
                }
            });

            var service = context.GetOrganizationService();

            // Act
            var accountUpdate = new Account
            {
                Id = accountId,
                Name = "Updated Name"
            };
            service.Update(accountUpdate);

            // Assert
            Assert.True(ImageTestPlugin.ReceivedImages.ContainsKey("FilteredPreImage"));
            var preImage = ImageTestPlugin.ReceivedImages["FilteredPreImage"];
            
            // Should contain filtered attributes
            Assert.True(preImage.Contains("name"));
            Assert.True(preImage.Contains("accountnumber"));
            
            // Should NOT contain non-filtered attributes
            Assert.False(preImage.Contains("telephone1"));
            Assert.False(preImage.Contains("revenue"));
        }

        [Fact]
        public void Should_CreatePostImage_ForUpdateMessage()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities
            // Post-images are available for Create and Update messages and capture the state after the operation
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            var accountId = Guid.NewGuid();
            var existingAccount = new Account
            {
                Id = accountId,
                Name = "Original Name",
                AccountNumber = "ACC-001"
            };
            context.Initialize(existingAccount);

            // Register plugin with post-image
            ImageTestPlugin.ReceivedImages.Clear();
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Update",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                PluginType = typeof(ImageTestPlugin),
                PostImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "PostImage",
                        EntityAlias = "PostImage",
                        ImageType = ProcessingStepImageType.PostImage,
                        Attributes = new HashSet<string>()
                    }
                }
            });

            var service = context.GetOrganizationService();

            // Act
            var accountUpdate = new Account
            {
                Id = accountId,
                Name = "Updated Name"
            };
            service.Update(accountUpdate);

            // Assert
            Assert.True(ImageTestPlugin.ReceivedImages.ContainsKey("PostImage"));
            var postImage = ImageTestPlugin.ReceivedImages["PostImage"];
            
            Assert.Equal("account", postImage.LogicalName);
            Assert.Equal(accountId, postImage.Id);
            // Post-image should reflect the updated state
            Assert.Equal("Updated Name", postImage.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_CreateMultipleImages_WithDifferentFilters()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities
            // Multiple images can be registered with different attribute filters
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            var accountId = Guid.NewGuid();
            var existingAccount = new Account
            {
                Id = accountId,
                Name = "Original Name",
                AccountNumber = "ACC-001",
                Telephone1 = "555-0100",
                Revenue = new Money(100000)
            };
            context.Initialize(existingAccount);

            // Register plugin with multiple pre-images
            ImageTestPlugin.ReceivedImages.Clear();
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Update",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(ImageTestPlugin),
                PreImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "NameImage",
                        EntityAlias = "NameImage",
                        ImageType = ProcessingStepImageType.PreImage,
                        Attributes = new HashSet<string> { "name" }
                    },
                    new PluginStepImageRegistration
                    {
                        Name = "FinancialImage",
                        EntityAlias = "FinancialImage",
                        ImageType = ProcessingStepImageType.PreImage,
                        Attributes = new HashSet<string> { "revenue", "accountnumber" }
                    }
                }
            });

            var service = context.GetOrganizationService();

            // Act
            var accountUpdate = new Account
            {
                Id = accountId,
                Name = "Updated Name"
            };
            service.Update(accountUpdate);

            // Assert
            Assert.Equal(2, ImageTestPlugin.ReceivedImages.Count);
            
            // Check NameImage
            Assert.True(ImageTestPlugin.ReceivedImages.ContainsKey("NameImage"));
            var nameImage = ImageTestPlugin.ReceivedImages["NameImage"];
            Assert.True(nameImage.Contains("name"));
            Assert.False(nameImage.Contains("revenue"));
            Assert.False(nameImage.Contains("accountnumber"));
            
            // Check FinancialImage
            Assert.True(ImageTestPlugin.ReceivedImages.ContainsKey("FinancialImage"));
            var finImage = ImageTestPlugin.ReceivedImages["FinancialImage"];
            Assert.True(finImage.Contains("revenue"));
            Assert.True(finImage.Contains("accountnumber"));
            Assert.False(finImage.Contains("name"));
        }

        [Fact]
        public void Should_CreateBothPreAndPostImages_ForUpdateMessage()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#when-to-use-pre-or-post-images
            // For Update messages, both pre and post images can be registered
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            var accountId = Guid.NewGuid();
            var existingAccount = new Account
            {
                Id = accountId,
                Name = "Original Name",
                AccountNumber = "ACC-001"
            };
            context.Initialize(existingAccount);

            // Register plugin with both pre and post images
            ImageTestPlugin.ReceivedImages.Clear();
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Update",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(ImageTestPlugin),
                PreImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "PreImage",
                        EntityAlias = "PreImage",
                        ImageType = ProcessingStepImageType.PreImage,
                        Attributes = new HashSet<string>()
                    }
                },
                PostImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "PostImage",
                        EntityAlias = "PostImage",
                        ImageType = ProcessingStepImageType.PostImage,
                        Attributes = new HashSet<string>()
                    }
                }
            });

            var service = context.GetOrganizationService();

            // Act
            var accountUpdate = new Account
            {
                Id = accountId,
                Name = "Updated Name"
            };
            service.Update(accountUpdate);

            // Assert
            Assert.True(ImageTestPlugin.ReceivedImages.ContainsKey("PreImage"));
            Assert.True(ImageTestPlugin.ReceivedImages.ContainsKey("PostImage"));
            
            var preImage = ImageTestPlugin.ReceivedImages["PreImage"];
            var postImage = ImageTestPlugin.ReceivedImages["PostImage"];
            
            // Pre-image should have original values
            Assert.Equal("Original Name", preImage.GetAttributeValue<string>("name"));
            
            // Post-image should have updated values
            Assert.Equal("Updated Name", postImage.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_NotCreatePreImage_ForCreateMessage()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#when-to-use-pre-or-post-images
            // Pre-images are NOT available for Create messages (entity doesn't exist before Create)
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            // Register plugin with pre-image (should be ignored for Create)
            ImageTestPlugin.ReceivedImages.Clear();
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(ImageTestPlugin),
                PreImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "PreImage",
                        EntityAlias = "PreImage",
                        ImageType = ProcessingStepImageType.PreImage,
                        Attributes = new HashSet<string>()
                    }
                }
            });

            var service = context.GetOrganizationService();

            // Act
            var newAccount = new Account
            {
                Name = "New Account"
            };
            service.Create(newAccount);

            // Assert - Pre-image should not be created for Create message
            Assert.False(ImageTestPlugin.ReceivedImages.ContainsKey("PreImage"));
        }

        [Fact]
        public void Should_CreatePostImage_ForCreateMessage()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#when-to-use-pre-or-post-images
            // Post-images ARE available for Create messages
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            // Register plugin with post-image
            ImageTestPlugin.ReceivedImages.Clear();
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                PluginType = typeof(ImageTestPlugin),
                PostImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "PostImage",
                        EntityAlias = "PostImage",
                        ImageType = ProcessingStepImageType.PostImage,
                        Attributes = new HashSet<string>()
                    }
                }
            });

            var service = context.GetOrganizationService();

            // Act
            var newAccount = new Account
            {
                Name = "New Account",
                AccountNumber = "ACC-NEW"
            };
            service.Create(newAccount);

            // Assert
            Assert.True(ImageTestPlugin.ReceivedImages.ContainsKey("PostImage"));
            var postImage = ImageTestPlugin.ReceivedImages["PostImage"];
            Assert.Equal("New Account", postImage.GetAttributeValue<string>("name"));
            Assert.Equal("ACC-NEW", postImage.GetAttributeValue<string>("accountnumber"));
        }

        [Fact]
        public void Should_CreatePreImage_ForDeleteMessage()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#when-to-use-pre-or-post-images
            // Pre-images ARE available for Delete messages
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            var accountId = Guid.NewGuid();
            var existingAccount = new Account
            {
                Id = accountId,
                Name = "Account To Delete",
                AccountNumber = "ACC-DEL"
            };
            context.Initialize(existingAccount);

            // Register plugin with pre-image
            ImageTestPlugin.ReceivedImages.Clear();
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Delete",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(ImageTestPlugin),
                PreImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "PreImage",
                        EntityAlias = "PreImage",
                        ImageType = ProcessingStepImageType.PreImage,
                        Attributes = new HashSet<string>()
                    }
                }
            });

            var service = context.GetOrganizationService();

            // Act
            service.Delete("account", accountId);

            // Assert
            Assert.True(ImageTestPlugin.ReceivedImages.ContainsKey("PreImage"));
            var preImage = ImageTestPlugin.ReceivedImages["PreImage"];
            Assert.Equal("Account To Delete", preImage.GetAttributeValue<string>("name"));
            Assert.Equal("ACC-DEL", preImage.GetAttributeValue<string>("accountnumber"));
        }

        [Fact]
        public void Should_NotCreatePostImage_ForDeleteMessage()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#when-to-use-pre-or-post-images
            // Post-images are NOT available for Delete messages (entity no longer exists after Delete)
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            var accountId = Guid.NewGuid();
            var existingAccount = new Account
            {
                Id = accountId,
                Name = "Account To Delete"
            };
            context.Initialize(existingAccount);

            // Register plugin with post-image (should be ignored for Delete)
            ImageTestPlugin.ReceivedImages.Clear();
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Delete",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                PluginType = typeof(ImageTestPlugin),
                PostImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "PostImage",
                        EntityAlias = "PostImage",
                        ImageType = ProcessingStepImageType.PostImage,
                        Attributes = new HashSet<string>()
                    }
                }
            });

            var service = context.GetOrganizationService();

            // Act
            service.Delete("account", accountId);

            // Assert - Post-image should not be created for Delete message
            Assert.False(ImageTestPlugin.ReceivedImages.ContainsKey("PostImage"));
        }

        [Fact]
        public void Should_UseImageTypeBoth_ToCreateBothImages()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities
            // ImageType.Both registers both pre and post images with the same configuration
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            var accountId = Guid.NewGuid();
            var existingAccount = new Account
            {
                Id = accountId,
                Name = "Original Name"
            };
            context.Initialize(existingAccount);

            // Register plugin with ImageType.Both
            ImageTestPlugin.ReceivedImages.Clear();
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Update",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(ImageTestPlugin),
                PreImages = new List<PluginStepImageRegistration>
                {
                    new PluginStepImageRegistration
                    {
                        Name = "Image",
                        EntityAlias = "Image",
                        ImageType = ProcessingStepImageType.Both,
                        Attributes = new HashSet<string>()
                    }
                }
            });

            var service = context.GetOrganizationService();

            // Act
            var accountUpdate = new Account
            {
                Id = accountId,
                Name = "Updated Name"
            };
            service.Update(accountUpdate);

            // Assert - Image should be in PreEntityImages
            Assert.True(ImageTestPlugin.ReceivedImages.ContainsKey("Image"));
        }
    }

    #region Test Plugins

    /// <summary>
    /// Test plugin for capturing entity images
    /// </summary>
    public class ImageTestPlugin : IPlugin
    {
        public static Dictionary<string, Entity> ReceivedImages { get; } = new Dictionary<string, Entity>();

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Capture all pre-images
            if (context.PreEntityImages != null)
            {
                foreach (var image in context.PreEntityImages)
                {
                    ReceivedImages[image.Key] = image.Value;
                }
            }

            // Capture all post-images
            if (context.PostEntityImages != null)
            {
                foreach (var image in context.PostEntityImages)
                {
                    ReceivedImages[image.Key] = image.Value;
                }
            }
        }
    }

    #endregion
}
