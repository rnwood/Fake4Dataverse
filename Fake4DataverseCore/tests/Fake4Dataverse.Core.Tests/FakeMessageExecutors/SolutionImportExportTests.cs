using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Crm;
using Fake4Dataverse;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests.FakeMessageExecutors
{
    /// <summary>
    /// Tests for ImportSolutionRequest and ExportSolutionRequest message executors
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.exportsolutionrequest
    /// 
    /// ImportSolutionRequest imports a solution into the organization from a ZIP file.
    /// ExportSolutionRequest exports a solution from the organization as a ZIP file.
    /// Solutions contain customizations and components like entities, forms, views, etc.
    /// </summary>
    public class SolutionImportExportTests : Fake4DataverseTests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public SolutionImportExportTests()
        {
            _context = base._context;
            _service = base._service;

            // Initialize basic solution infrastructure without CDM files
            InitializeSolutionInfrastructure();
        }

        /// <summary>
        /// Initializes the solution infrastructure with necessary tables and metadata
        /// </summary>
        private void InitializeSolutionInfrastructure()
        {
            // Create a default publisher manually
            var publisher = new Entity("publisher")
            {
                Id = Guid.NewGuid()
            };
            publisher["publisherid"] = publisher.Id;
            publisher["uniquename"] = "DefaultPublisher";
            publisher["friendlyname"] = "Default Publisher";
            publisher["customizationprefix"] = "new";
            publisher["customizationoptionvalueprefix"] = 10000;
            
            // Add to context data directly to avoid needing publisher metadata
            var xrmContext = _context as XrmFakedContext;
            if (xrmContext != null)
            {
                if (!xrmContext.Data.ContainsKey("publisher"))
                {
                    xrmContext.Data["publisher"] = new System.Collections.Generic.Dictionary<Guid, Entity>();
                }
                xrmContext.Data["publisher"][publisher.Id] = publisher;
            }
        }

        /// <summary>
        /// Test: ImportSolution throws exception when CustomizationFile is null
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest.customizationfile
        /// 
        /// CustomizationFile property is required and contains the solution ZIP file as a byte array.
        /// The import should fail if this property is not provided.
        /// </summary>
        [Fact]
        public void Should_Throw_Exception_When_CustomizationFile_Is_Null()
        {
            // Arrange
            var request = new ImportSolutionRequest
            {
                CustomizationFile = null
            };

            // Act & Assert
            var exception = Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() => 
                _service.Execute(request));
            Assert.Contains("CustomizationFile is required", exception.Message);
        }

        /// <summary>
        /// Test: ImportSolution throws exception when CustomizationFile is empty
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest.customizationfile
        /// </summary>
        [Fact]
        public void Should_Throw_Exception_When_CustomizationFile_Is_Empty()
        {
            // Arrange
            var request = new ImportSolutionRequest
            {
                CustomizationFile = new byte[0]
            };

            // Act & Assert
            var exception = Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() => 
                _service.Execute(request));
            Assert.Contains("CustomizationFile is required", exception.Message);
        }

        /// <summary>
        /// Test: ImportSolution throws exception for invalid ZIP file
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/solution-file-reference
        /// 
        /// The CustomizationFile must be a valid ZIP archive. Invalid ZIP data should fail.
        /// </summary>
        [Fact]
        public void Should_Throw_Exception_For_Invalid_ZIP_File()
        {
            // Arrange
            var invalidZipData = Encoding.UTF8.GetBytes("This is not a ZIP file");
            var request = new ImportSolutionRequest
            {
                CustomizationFile = invalidZipData
            };

            // Act & Assert
            var exception = Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() => 
                _service.Execute(request));
            Assert.Contains("Invalid solution file format", exception.Message);
        }

        /// <summary>
        /// Test: ImportSolution throws exception when solution.xml is missing
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/solution-file-reference
        /// 
        /// Every solution ZIP file must contain a solution.xml manifest file.
        /// The import should fail if this required file is missing.
        /// </summary>
        [Fact]
        public void Should_Throw_Exception_When_Solution_Xml_Missing()
        {
            // Arrange
            var solutionZip = CreateEmptyZipFile();
            var request = new ImportSolutionRequest
            {
                CustomizationFile = solutionZip
            };

            // Act & Assert
            var exception = Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() => 
                _service.Execute(request));
            Assert.Contains("solution.xml not found", exception.Message);
        }

        /// <summary>
        /// Test: ImportSolution creates new solution when it doesn't exist
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest
        /// 
        /// When importing a solution that doesn't exist in the organization, a new solution
        /// record should be created with the metadata from solution.xml.
        /// </summary>
        [Fact]
        public void Should_Create_New_Solution_On_First_Import()
        {
            // Arrange
            var solutionZip = CreateTestSolution("TestSolution", "1.0.0.0", false);
            var request = new ImportSolutionRequest
            {
                CustomizationFile = solutionZip
            };

            // Act
            var response = (ImportSolutionResponse)_service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotEqual(Guid.Empty, response.Results["ImportJobId"]);

            // Verify solution was created
            var solutions = _service.RetrieveMultiple(new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("uniquename", "version", "friendlyname", "ismanaged"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, "TestSolution")
                    }
                }
            });

            Assert.Single(solutions.Entities);
            var solution = solutions.Entities[0];
            Assert.Equal("TestSolution", solution.GetAttributeValue<string>("uniquename"));
            Assert.Equal("1.0.0.0", solution.GetAttributeValue<string>("version"));
            Assert.Equal("Test Solution", solution.GetAttributeValue<string>("friendlyname"));
            Assert.False(solution.GetAttributeValue<bool>("ismanaged"));
        }

        /// <summary>
        /// Test: ImportSolution updates existing solution
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest
        /// 
        /// When importing a solution that already exists, the existing solution record
        /// should be updated with the new metadata.
        /// </summary>
        [Fact]
        public void Should_Update_Existing_Solution_On_Reimport()
        {
            // Arrange - First import
            var solutionZip1 = CreateTestSolution("TestSolution", "1.0.0.0", false);
            var request1 = new ImportSolutionRequest
            {
                CustomizationFile = solutionZip1
            };
            _service.Execute(request1);

            // Get the solution ID
            var solutions = _service.RetrieveMultiple(new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid", "version"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, "TestSolution")
                    }
                }
            });
            var originalSolutionId = solutions.Entities[0].Id;
            Assert.Equal("1.0.0.0", solutions.Entities[0].GetAttributeValue<string>("version"));

            // Act - Second import with different version
            var solutionZip2 = CreateTestSolution("TestSolution", "2.0.0.0", false);
            var request2 = new ImportSolutionRequest
            {
                CustomizationFile = solutionZip2
            };
            _service.Execute(request2);

            // Assert
            var updatedSolutions = _service.RetrieveMultiple(new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid", "version"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, "TestSolution")
                    }
                }
            });

            // Should still be only one solution with the same ID
            Assert.Single(updatedSolutions.Entities);
            Assert.Equal(originalSolutionId, updatedSolutions.Entities[0].Id);
            Assert.Equal("2.0.0.0", updatedSolutions.Entities[0].GetAttributeValue<string>("version"));
        }

        /// <summary>
        /// Test: ImportSolution prevents importing unmanaged over managed solution
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest
        /// 
        /// You cannot import an unmanaged solution over a managed solution. This should
        /// result in an error with ErrorCode = ImportSolutionManagedToUnmanagedMismatch.
        /// </summary>
        [Fact]
        public void Should_Prevent_Importing_Unmanaged_Over_Managed_Solution()
        {
            // Arrange - Import managed solution first
            var managedSolutionZip = CreateTestSolution("TestSolution", "1.0.0.0", true);
            var request1 = new ImportSolutionRequest
            {
                CustomizationFile = managedSolutionZip
            };
            _service.Execute(request1);

            // Act - Try to import unmanaged over managed
            var unmanagedSolutionZip = CreateTestSolution("TestSolution", "2.0.0.0", false);
            var request2 = new ImportSolutionRequest
            {
                CustomizationFile = unmanagedSolutionZip
            };

            // Assert
            var exception = Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() => 
                _service.Execute(request2));
            Assert.Contains("Cannot import unmanaged solution over a managed solution", exception.Message);
        }

        /// <summary>
        /// Test: ImportSolution with ConvertToManaged creates managed solution
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest.converttomanaged
        /// 
        /// The ConvertToManaged property allows importing an unmanaged solution as managed.
        /// When set to true, the solution should be imported as managed regardless of the
        /// Managed flag in solution.xml.
        /// </summary>
        [Fact]
        public void Should_Convert_To_Managed_When_ConvertToManaged_Is_True()
        {
            // Arrange
            var unmanagedSolutionZip = CreateTestSolution("TestSolution", "1.0.0.0", false);
            var request = new ImportSolutionRequest
            {
                CustomizationFile = unmanagedSolutionZip,
                ConvertToManaged = true
            };

            // Act
            _service.Execute(request);

            // Assert
            var solutions = _service.RetrieveMultiple(new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("ismanaged"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, "TestSolution")
                    }
                }
            });

            Assert.Single(solutions.Entities);
            Assert.True(solutions.Entities[0].GetAttributeValue<bool>("ismanaged"));
        }

        /// <summary>
        /// Test: ExportSolution throws exception when SolutionName is null
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.exportsolutionrequest.solutionname
        /// 
        /// SolutionName property is required and specifies the unique name of the solution to export.
        /// The export should fail if this property is not provided.
        /// </summary>
        [Fact]
        public void Should_Throw_Exception_When_SolutionName_Is_Null()
        {
            // Arrange
            var request = new ExportSolutionRequest
            {
                SolutionName = null
            };

            // Act & Assert
            var exception = Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() => 
                _service.Execute(request));
            Assert.Contains("SolutionName is required", exception.Message);
        }

        /// <summary>
        /// Test: ExportSolution throws exception when solution doesn't exist
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.exportsolutionrequest
        /// 
        /// The export should fail if the specified solution unique name doesn't exist.
        /// </summary>
        [Fact]
        public void Should_Throw_Exception_When_Solution_Does_Not_Exist()
        {
            // Arrange
            var request = new ExportSolutionRequest
            {
                SolutionName = "NonExistentSolution"
            };

            // Act & Assert
            var exception = Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() => 
                _service.Execute(request));
            Assert.Contains("not found", exception.Message);
        }

        /// <summary>
        /// Test: ExportSolution creates valid ZIP file with solution.xml
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.exportsolutionrequest
        /// 
        /// ExportSolution should create a valid ZIP archive containing at least:
        /// - solution.xml: The solution manifest
        /// - [Content_Types].xml: MIME type definitions
        /// </summary>
        [Fact]
        public void Should_Export_Valid_Solution_ZIP_File()
        {
            // Arrange - Create a solution
            var solution = new Entity("solution")
            {
                Id = Guid.NewGuid()
            };
            solution["solutionid"] = solution.Id;
            solution["uniquename"] = "TestSolution";
            solution["friendlyname"] = "Test Solution";
            solution["version"] = "1.0.0.0";
            solution["ismanaged"] = false;
            _service.Create(solution);

            var request = new ExportSolutionRequest
            {
                SolutionName = "TestSolution",
                Managed = false
            };

            // Act
            var response = (ExportSolutionResponse)_service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var exportedFile = response.Results["ExportSolutionFile"] as byte[];
            Assert.NotNull(exportedFile);
            Assert.NotEmpty(exportedFile);

            // Verify it's a valid ZIP file
            using (var stream = new MemoryStream(exportedFile))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                // Should have solution.xml
                var solutionXmlEntry = zip.Entries.FirstOrDefault(e => 
                    e.FullName.Equals("solution.xml", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(solutionXmlEntry);

                // Should have [Content_Types].xml
                var contentTypesEntry = zip.Entries.FirstOrDefault(e => 
                    e.FullName.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(contentTypesEntry);

                // Verify solution.xml content
                using (var solutionXmlStream = solutionXmlEntry.Open())
                {
                    var solutionXml = XDocument.Load(solutionXmlStream);
                    var uniqueNameElement = solutionXml.Descendants("UniqueName").FirstOrDefault();
                    Assert.NotNull(uniqueNameElement);
                    Assert.Equal("TestSolution", uniqueNameElement.Value);
                }
            }
        }

        /// <summary>
        /// Test: ExportSolution with Managed=true exports as managed solution
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.exportsolutionrequest.managed
        /// 
        /// The Managed property controls whether the solution is exported as managed.
        /// When true, the Managed element in solution.xml should be "1".
        /// </summary>
        [Fact]
        public void Should_Export_As_Managed_When_Managed_Is_True()
        {
            // Arrange - Create an unmanaged solution
            var solution = new Entity("solution")
            {
                Id = Guid.NewGuid()
            };
            solution["solutionid"] = solution.Id;
            solution["uniquename"] = "TestSolution";
            solution["friendlyname"] = "Test Solution";
            solution["version"] = "1.0.0.0";
            solution["ismanaged"] = false; // Unmanaged in database
            _service.Create(solution);

            var request = new ExportSolutionRequest
            {
                SolutionName = "TestSolution",
                Managed = true // Export as managed
            };

            // Act
            var response = (ExportSolutionResponse)_service.Execute(request);

            // Assert
            var exportedFile = response.Results["ExportSolutionFile"] as byte[];
            using (var stream = new MemoryStream(exportedFile))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var solutionXmlEntry = zip.Entries.First(e => 
                    e.FullName.Equals("solution.xml", StringComparison.OrdinalIgnoreCase));
                using (var solutionXmlStream = solutionXmlEntry.Open())
                {
                    var solutionXml = XDocument.Load(solutionXmlStream);
                    var managedElement = solutionXml.Descendants("Managed").FirstOrDefault();
                    Assert.NotNull(managedElement);
                    Assert.Equal("1", managedElement.Value);
                }
            }
        }

        /// <summary>
        /// Test: Import and Export roundtrip preserves solution metadata
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/work-with-solutions
        /// 
        /// A solution should be able to be exported and then imported again, preserving
        /// all metadata including unique name, version, and managed status.
        /// </summary>
        [Fact]
        public void Should_Preserve_Solution_Metadata_In_Import_Export_Roundtrip()
        {
            // Arrange - Import a solution
            var originalSolutionZip = CreateTestSolution("TestSolution", "1.0.0.0", false);
            var importRequest = new ImportSolutionRequest
            {
                CustomizationFile = originalSolutionZip
            };
            _service.Execute(importRequest);

            // Act - Export the solution
            var exportRequest = new ExportSolutionRequest
            {
                SolutionName = "TestSolution",
                Managed = false
            };
            var exportResponse = (ExportSolutionResponse)_service.Execute(exportRequest);
            var exportedFile = exportResponse.Results["ExportSolutionFile"] as byte[];

            // Delete the solution
            var solutions = _service.RetrieveMultiple(new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, "TestSolution")
                    }
                }
            });
            _service.Delete("solution", solutions.Entities[0].Id);

            // Re-import the exported solution
            var reimportRequest = new ImportSolutionRequest
            {
                CustomizationFile = exportedFile
            };
            _service.Execute(reimportRequest);

            // Assert - Verify metadata is preserved
            var reimportedSolutions = _service.RetrieveMultiple(new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("uniquename", "version", "friendlyname", "ismanaged"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, "TestSolution")
                    }
                }
            });

            Assert.Single(reimportedSolutions.Entities);
            var reimportedSolution = reimportedSolutions.Entities[0];
            Assert.Equal("TestSolution", reimportedSolution.GetAttributeValue<string>("uniquename"));
            Assert.Equal("1.0.0.0", reimportedSolution.GetAttributeValue<string>("version"));
            Assert.Equal("Test Solution", reimportedSolution.GetAttributeValue<string>("friendlyname"));
            Assert.False(reimportedSolution.GetAttributeValue<bool>("ismanaged"));
        }

        #region Helper Methods

        /// <summary>
        /// Creates an empty ZIP file for testing
        /// </summary>
        private byte[] CreateEmptyZipFile()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // Empty ZIP file
                }
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Creates a test solution ZIP file
        /// </summary>
        private byte[] CreateTestSolution(string uniqueName, string version, bool isManaged)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // Create solution.xml
                    var solutionXml = new XDocument(
                        new XDeclaration("1.0", "utf-8", null),
                        new XElement("ImportExportXml",
                            new XAttribute("version", "9.0.0.0"),
                            new XElement("SolutionManifest",
                                new XElement("UniqueName", uniqueName),
                                new XElement("LocalizedNames",
                                    new XElement("LocalizedName",
                                        new XAttribute("description", "Test Solution"),
                                        new XAttribute("languagecode", "1033")
                                    )
                                ),
                                new XElement("Descriptions"),
                                new XElement("Version", version),
                                new XElement("Publisher",
                                    new XElement("UniqueName", "DefaultPublisher"),
                                    new XElement("LocalizedNames",
                                        new XElement("LocalizedName",
                                            new XAttribute("description", "Default Publisher"),
                                            new XAttribute("languagecode", "1033")
                                        )
                                    ),
                                    new XElement("Descriptions"),
                                    new XElement("EMailAddress"),
                                    new XElement("SupportingWebsiteUrl"),
                                    new XElement("CustomizationPrefix", "new"),
                                    new XElement("CustomizationOptionValuePrefix", "10000")
                                ),
                                new XElement("Managed", isManaged ? "1" : "0"),
                                new XElement("RootComponents")
                            )
                        )
                    );

                    var solutionXmlEntry = zipArchive.CreateEntry("solution.xml");
                    using (var entryStream = solutionXmlEntry.Open())
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        solutionXml.Save(writer);
                    }

                    // Create [Content_Types].xml
                    var contentTypesXml = new XDocument(
                        new XDeclaration("1.0", "utf-8", null),
                        new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types") + "Types",
                            new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types") + "Default",
                                new XAttribute("Extension", "xml"),
                                new XAttribute("ContentType", "application/xml")
                            )
                        )
                    );

                    var contentTypesEntry = zipArchive.CreateEntry("[Content_Types].xml");
                    using (var entryStream = contentTypesEntry.Open())
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        contentTypesXml.Save(writer);
                    }
                }

                return memoryStream.ToArray();
            }
        }

        #endregion

        #region ImportSolutions Tests

        /// <summary>
        /// Test: ImportSolutions successfully imports multiple solution files
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest
        /// </summary>
        [Fact]
        public void Should_Import_Multiple_Solutions_Successfully()
        {
            // Arrange
            var solution1 = CreateTestSolution("TestSolution1", "1.0.0.0", false);
            var solution2 = CreateTestSolution("TestSolution2", "2.0.0.0", false);

            // Act
            var xrmContext = _context as XrmFakedContext;
            xrmContext.ImportSolutions(new byte[][] { solution1, solution2 });

            // Assert
            var query1 = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("uniquename"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, "TestSolution1") }
                }
            };
            var solutions1 = _service.RetrieveMultiple(query1);
            Assert.Single(solutions1.Entities);

            var query2 = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("uniquename"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, "TestSolution2") }
                }
            };
            var solutions2 = _service.RetrieveMultiple(query2);
            Assert.Single(solutions2.Entities);
        }

        /// <summary>
        /// Test: ImportSolutions throws exception when solution array is null
        /// </summary>
        [Fact]
        public void Should_Throw_Exception_When_ImportSolutions_Array_Is_Null()
        {
            // Arrange
            var xrmContext = _context as XrmFakedContext;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => xrmContext.ImportSolutions(null));
            Assert.Contains("solutionFiles", exception.Message);
        }

        /// <summary>
        /// Test: ImportSolutions throws exception when any solution file is invalid
        /// </summary>
        [Fact]
        public void Should_Throw_Exception_When_ImportSolutions_Contains_Invalid_File()
        {
            // Arrange
            var solution1 = CreateTestSolution("TestSolution1", "1.0.0.0", false);
            var invalidSolution = new byte[] { 1, 2, 3, 4, 5 }; // Invalid ZIP

            var xrmContext = _context as XrmFakedContext;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                xrmContext.ImportSolutions(new byte[][] { solution1, invalidSolution }));
            Assert.Contains("Failed to import solution at index 1", exception.Message);
        }

        /// <summary>
        /// Test: ImportSolutions stops on first error
        /// </summary>
        [Fact]
        public void Should_Stop_ImportSolutions_On_First_Error()
        {
            // Arrange
            var solution1 = CreateTestSolution("TestSolution1", "1.0.0.0", false);
            var invalidSolution = new byte[] { 1, 2, 3, 4, 5 }; // Invalid ZIP
            var solution3 = CreateTestSolution("TestSolution3", "3.0.0.0", false);

            var xrmContext = _context as XrmFakedContext;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                xrmContext.ImportSolutions(new byte[][] { solution1, invalidSolution, solution3 }));
            
            Assert.Contains("Failed to import solution at index 1", exception.Message);
            Assert.Contains("1 solution(s) not imported", exception.Message);

            // Verify solution3 was not imported
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("uniquename"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, "TestSolution3") }
                }
            };
            var solutions = _service.RetrieveMultiple(query);
            Assert.Empty(solutions.Entities);
        }

        #endregion
    }
}
