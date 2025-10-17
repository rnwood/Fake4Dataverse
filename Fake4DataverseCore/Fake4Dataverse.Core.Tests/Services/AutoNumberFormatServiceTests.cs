using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Fake4Dataverse.Services;

namespace Fake4Dataverse.Tests.Services
{
    /// <summary>
    /// Tests for AutoNumberFormatService.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
    /// Auto number fields automatically generate alphanumeric strings when a new record is created.
    /// </summary>
    public class AutoNumberFormatServiceTests
    {
        [Fact]
        public void Should_Generate_Sequential_Numbers_With_Leading_Zeros()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#sequential-number
            // {SEQNUM:n} generates a sequential number with n digits, padded with leading zeros.
            // The sequence starts at 1 and increments by 1 for each new record.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "TEST-{SEQNUM:5}";
            
            // Act
            var result1 = service.GenerateAutoNumber("testentity", "testnumber", format);
            var result2 = service.GenerateAutoNumber("testentity", "testnumber", format);
            var result3 = service.GenerateAutoNumber("testentity", "testnumber", format);
            
            // Assert
            Assert.Equal("TEST-00001", result1);
            Assert.Equal("TEST-00002", result2);
            Assert.Equal("TEST-00003", result3);
        }

        [Fact]
        public void Should_Maintain_Separate_Sequences_Per_Entity_And_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#sequential-number
            // Each entity and attribute combination maintains its own independent sequence.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "{SEQNUM:3}";
            
            // Act
            var entity1Result1 = service.GenerateAutoNumber("entity1", "attr1", format);
            var entity1Result2 = service.GenerateAutoNumber("entity1", "attr1", format);
            var entity2Result1 = service.GenerateAutoNumber("entity2", "attr1", format);
            var entity1Attr2Result1 = service.GenerateAutoNumber("entity1", "attr2", format);
            
            // Assert
            Assert.Equal("001", entity1Result1);
            Assert.Equal("002", entity1Result2);
            Assert.Equal("001", entity2Result1); // Different entity, new sequence
            Assert.Equal("001", entity1Attr2Result1); // Same entity but different attribute, new sequence
        }

        [Fact]
        public void Should_Generate_Random_Alphanumeric_Strings()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#random-string
            // {RANDSTRING:n} generates a random alphanumeric string with n characters.
            // Uses uppercase letters and numbers, excluding ambiguous characters.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "CASE-{RANDSTRING:5}";
            
            // Act
            var result1 = service.GenerateAutoNumber("testentity", "testnumber", format);
            var result2 = service.GenerateAutoNumber("testentity", "testnumber", format);
            
            // Assert
            Assert.StartsWith("CASE-", result1);
            Assert.StartsWith("CASE-", result2);
            Assert.Equal(10, result1.Length); // CASE- (5) + random (5)
            Assert.Equal(10, result2.Length);
            
            // Extract random parts
            var random1 = result1.Substring(5);
            var random2 = result2.Substring(5);
            
            // Random strings should be different (extremely high probability)
            Assert.NotEqual(random1, random2);
            
            // All characters should be alphanumeric uppercase
            Assert.Matches("^[A-Z0-9]+$", random1);
            Assert.Matches("^[A-Z0-9]+$", random2);
        }

        [Fact]
        public void Should_Format_Date_Time_UTC()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#date-and-time
            // {DATETIMEUTC:format} generates the current UTC date/time using the specified format string.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "TICKET-{DATETIMEUTC:yyyyMMdd}-{SEQNUM:3}";
            
            // Act
            var result = service.GenerateAutoNumber("testentity", "testnumber", format);
            
            // Assert
            var expectedDate = DateTime.UtcNow.ToString("yyyyMMdd");
            Assert.StartsWith($"TICKET-{expectedDate}-", result);
            Assert.EndsWith("-001", result);
        }

        [Fact]
        public void Should_Format_Date_Time_With_Custom_Format()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#date-and-time
            // Date/time formats support standard .NET DateTime format strings.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "ORDER-{DATETIMEUTC:yyyy-MM-dd HH:mm}";
            
            // Act
            var result = service.GenerateAutoNumber("testentity", "testnumber", format);
            
            // Assert
            var expectedPrefix = $"ORDER-{DateTime.UtcNow:yyyy-MM-dd HH:mm}";
            // Allow for potential minute boundary crossing during test execution
            Assert.Matches(@"ORDER-\d{4}-\d{2}-\d{2} \d{2}:\d{2}", result);
        }

        [Fact]
        public void Should_Support_Complex_Format_With_Multiple_Tokens()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // Format patterns can combine multiple token types with static text.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "CAR-{DATETIMEUTC:yyyyMMdd}-{SEQNUM:5}-{RANDSTRING:3}";
            
            // Act
            var result1 = service.GenerateAutoNumber("testentity", "testnumber", format);
            var result2 = service.GenerateAutoNumber("testentity", "testnumber", format);
            
            // Assert
            var expectedDate = DateTime.UtcNow.ToString("yyyyMMdd");
            Assert.Matches($@"CAR-{expectedDate}-00001-[A-Z0-9]{{3}}", result1);
            Assert.Matches($@"CAR-{expectedDate}-00002-[A-Z0-9]{{3}}", result2);
            
            // Sequential numbers should increment
            Assert.Contains("-00001-", result1);
            Assert.Contains("-00002-", result2);
        }

        [Fact]
        public void Should_Support_Static_Text_Only()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // Format patterns can be just static text without any tokens.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "STATIC-VALUE";
            
            // Act
            var result1 = service.GenerateAutoNumber("testentity", "testnumber", format);
            var result2 = service.GenerateAutoNumber("testentity", "testnumber", format);
            
            // Assert
            Assert.Equal("STATIC-VALUE", result1);
            Assert.Equal("STATIC-VALUE", result2);
        }

        [Fact]
        public void Should_Reset_Sequence_For_Entity_And_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#seed-values
            // Sequences can be reset for testing purposes.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "TEST-{SEQNUM:3}";
            
            // Act
            var result1 = service.GenerateAutoNumber("testentity", "testnumber", format);
            var result2 = service.GenerateAutoNumber("testentity", "testnumber", format);
            
            service.ResetSequence("testentity", "testnumber");
            
            var result3 = service.GenerateAutoNumber("testentity", "testnumber", format);
            
            // Assert
            Assert.Equal("TEST-001", result1);
            Assert.Equal("TEST-002", result2);
            Assert.Equal("TEST-001", result3); // Reset back to 1
        }

        [Fact]
        public void Should_Set_Sequence_Seed_Value()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#seed-values
            // Administrators can set the seed value for auto number sequences.
            // The next generated value will be seedValue + 1.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "TEST-{SEQNUM:5}";
            
            // Act
            service.SetSequenceSeed("testentity", "testnumber", 100);
            var result1 = service.GenerateAutoNumber("testentity", "testnumber", format);
            var result2 = service.GenerateAutoNumber("testentity", "testnumber", format);
            
            // Assert
            Assert.Equal("TEST-00101", result1);
            Assert.Equal("TEST-00102", result2);
        }

        [Fact]
        public void Should_Handle_Multiple_SEQNUM_Tokens()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // Format can contain multiple sequential number tokens.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "{SEQNUM:3}-{SEQNUM:2}";
            
            // Act
            var result1 = service.GenerateAutoNumber("testentity", "testnumber", format);
            var result2 = service.GenerateAutoNumber("testentity", "testnumber", format);
            
            // Assert
            Assert.Equal("001-02", result1);
            Assert.Equal("003-04", result2);
        }

        [Fact]
        public void Should_Use_Default_Format_When_Pattern_Is_Empty()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // If no format is specified, a default format is used.
            
            // Arrange
            var service = new AutoNumberFormatService();
            
            // Act
            var result1 = service.GenerateAutoNumber("testentity", "testnumber", null);
            var result2 = service.GenerateAutoNumber("testentity", "testnumber", "");
            
            // Assert
            Assert.Matches(@"TESTENTITY-\d{5}", result1);
            Assert.Matches(@"TESTENTITY-\d{5}", result2);
        }

        [Fact]
        public void Should_Return_Default_Format_For_Invoice()
        {
            // Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/invoice
            // Standard Dataverse entities have predefined auto number formats.
            
            // Arrange & Act
            var format = AutoNumberFormatService.GetDefaultFormatForEntity("invoice", "invoicenumber");
            
            // Assert
            Assert.Equal("INV-{SEQNUM:5}", format);
        }

        [Fact]
        public void Should_Return_Default_Format_For_Quote()
        {
            // Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/quote
            
            // Arrange & Act
            var format = AutoNumberFormatService.GetDefaultFormatForEntity("quote", "quotenumber");
            
            // Assert
            Assert.Equal("QUO-{SEQNUM:5}", format);
        }

        [Fact]
        public void Should_Return_Default_Format_For_Sales_Order()
        {
            // Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/salesorder
            
            // Arrange & Act
            var format = AutoNumberFormatService.GetDefaultFormatForEntity("salesorder", "ordernumber");
            
            // Assert
            Assert.Equal("ORD-{SEQNUM:5}", format);
        }

        [Fact]
        public void Should_Return_Default_Format_For_Case()
        {
            // Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/incident
            
            // Arrange & Act
            var format = AutoNumberFormatService.GetDefaultFormatForEntity("incident", "ticketnumber");
            
            // Assert
            Assert.Equal("CAS-{SEQNUM:5}", format);
        }

        [Fact]
        public void Should_Return_Generic_Format_For_Unknown_Entity()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // Custom entities use a generic format if not specifically configured.
            
            // Arrange & Act
            var format = AutoNumberFormatService.GetDefaultFormatForEntity("customentity", "customnumber");
            
            // Assert
            Assert.Equal("CUSTOMENTITY-{SEQNUM:5}", format);
        }

        [Fact]
        public void Should_Be_Thread_Safe_For_Sequential_Numbers()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // Auto number generation must be thread-safe to prevent duplicate values.
            
            // Arrange
            var service = new AutoNumberFormatService();
            var format = "TEST-{SEQNUM:5}";
            var results = new System.Collections.Concurrent.ConcurrentBag<string>();
            var tasks = new List<System.Threading.Tasks.Task>();
            
            // Act - Generate 100 numbers concurrently
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    var result = service.GenerateAutoNumber("testentity", "testnumber", format);
                    results.Add(result);
                }));
            }
            
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            
            // Assert - All values should be unique
            Assert.Equal(100, results.Count);
            Assert.Equal(100, results.Distinct().Count());
            
            // All values should be in valid format
            foreach (var result in results)
            {
                Assert.Matches(@"TEST-\d{5}", result);
            }
        }

        [Fact]
        public void Should_Handle_Case_Insensitive_Tokens()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // Tokens should be case-insensitive for flexibility.
            
            // Arrange
            var service = new AutoNumberFormatService();
            
            // Act
            var result1 = service.GenerateAutoNumber("testentity", "attr1", "TEST-{seqnum:3}");
            var result2 = service.GenerateAutoNumber("testentity", "attr2", "TEST-{SeqNum:3}");
            var result3 = service.GenerateAutoNumber("testentity", "attr3", "TEST-{RANDSTRING:3}");
            var result4 = service.GenerateAutoNumber("testentity", "attr4", "TEST-{randstring:3}");
            
            // Assert
            Assert.Equal("TEST-001", result1);
            Assert.Equal("TEST-001", result2);
            Assert.Matches(@"TEST-[A-Z0-9]{3}", result3);
            Assert.Matches(@"TEST-[A-Z0-9]{3}", result4);
        }
    }
}
