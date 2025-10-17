using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Services
{
    /// <summary>
    /// Service for generating auto number values based on Dataverse auto number format patterns.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
    /// 
    /// Auto number fields automatically generate alphanumeric strings when a new record is created.
    /// The format pattern can include:
    /// - Static text (prefixes/suffixes)
    /// - {SEQNUM:n} - Sequential number with n digits (e.g., {SEQNUM:5} generates 00001, 00002, etc.)
    /// - {RANDSTRING:n} - Random alphanumeric string with n characters (uppercase letters and numbers)
    /// - {DATETIMEUTC:format} - Current UTC date/time with custom format (e.g., {DATETIMEUTC:yyyyMMdd})
    /// - {DATETIMELOCAL:format} - Current local date/time with custom format
    /// 
    /// Example format: "CASE-{SEQNUM:5}-{RANDSTRING:3}" generates "CASE-00001-A3X", "CASE-00002-B7Y", etc.
    /// </summary>
    public class AutoNumberFormatService
    {
        // Track sequence numbers per entity/attribute combination
        private readonly ConcurrentDictionary<string, long> _sequenceCounters = new ConcurrentDictionary<string, long>();
        
        // Lock objects for thread-safe sequence generation
        private readonly ConcurrentDictionary<string, object> _sequenceLocks = new ConcurrentDictionary<string, object>();
        
        // Random generator for RANDSTRING tokens
        private static readonly Random _random = new Random();
        private static readonly object _randomLock = new object();
        
        // Characters used in random strings (uppercase letters and digits, excluding ambiguous chars)
        private const string RandomChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        /// <summary>
        /// Generates an auto number value based on the specified format pattern.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the entity</param>
        /// <param name="attributeLogicalName">The logical name of the attribute</param>
        /// <param name="formatPattern">The auto number format pattern (e.g., "CASE-{SEQNUM:5}")</param>
        /// <returns>Generated auto number value</returns>
        public string GenerateAutoNumber(string entityLogicalName, string attributeLogicalName, string formatPattern)
        {
            if (string.IsNullOrWhiteSpace(formatPattern))
            {
                // Default format if not specified: entity prefix + sequential number
                formatPattern = $"{entityLogicalName.ToUpper()}-{{SEQNUM:5}}";
            }

            var result = formatPattern;
            
            // Process {SEQNUM:n} tokens
            result = ProcessSequentialNumbers(result, entityLogicalName, attributeLogicalName);
            
            // Process {RANDSTRING:n} tokens
            result = ProcessRandomStrings(result);
            
            // Process {DATETIMEUTC:format} tokens
            result = ProcessDateTimeUtc(result);
            
            // Process {DATETIMELOCAL:format} tokens (for testing purposes, use UTC)
            result = ProcessDateTimeLocal(result);
            
            return result;
        }

        /// <summary>
        /// Processes {SEQNUM:n} tokens in the format pattern.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#sequential-number
        /// {SEQNUM:n} generates a sequential number with n digits, padded with leading zeros.
        /// The sequence is maintained per entity and attribute combination.
        /// </summary>
        private string ProcessSequentialNumbers(string pattern, string entityLogicalName, string attributeLogicalName)
        {
            var seqNumRegex = new Regex(@"\{SEQNUM:(\d+)\}", RegexOptions.IgnoreCase);
            
            return seqNumRegex.Replace(pattern, match =>
            {
                var digits = int.Parse(match.Groups[1].Value);
                var key = $"{entityLogicalName}.{attributeLogicalName}";
                
                // Get or create lock for this sequence
                var lockObj = _sequenceLocks.GetOrAdd(key, _ => new object());
                
                lock (lockObj)
                {
                    // Get next sequence number (starts at 1)
                    var nextNumber = _sequenceCounters.AddOrUpdate(key, 1, (k, v) => v + 1);
                    
                    // Format with leading zeros
                    return nextNumber.ToString($"D{digits}");
                }
            });
        }

        /// <summary>
        /// Processes {RANDSTRING:n} tokens in the format pattern.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#random-string
        /// {RANDSTRING:n} generates a random alphanumeric string with n characters.
        /// Uses uppercase letters and numbers, excluding ambiguous characters (I, O, 0, 1).
        /// </summary>
        private string ProcessRandomStrings(string pattern)
        {
            var randStringRegex = new Regex(@"\{RANDSTRING:(\d+)\}", RegexOptions.IgnoreCase);
            
            return randStringRegex.Replace(pattern, match =>
            {
                var length = int.Parse(match.Groups[1].Value);
                
                lock (_randomLock)
                {
                    var chars = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        chars[i] = RandomChars[_random.Next(RandomChars.Length)];
                    }
                    return new string(chars);
                }
            });
        }

        /// <summary>
        /// Processes {DATETIMEUTC:format} tokens in the format pattern.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#date-and-time
        /// {DATETIMEUTC:format} generates the current UTC date/time using the specified format string.
        /// Format follows standard .NET DateTime format strings (e.g., yyyyMMdd, yyyy-MM-dd HH:mm:ss).
        /// </summary>
        private string ProcessDateTimeUtc(string pattern)
        {
            var dateTimeRegex = new Regex(@"\{DATETIMEUTC:([^\}]+)\}", RegexOptions.IgnoreCase);
            
            return dateTimeRegex.Replace(pattern, match =>
            {
                var format = match.Groups[1].Value;
                return DateTime.UtcNow.ToString(format);
            });
        }

        /// <summary>
        /// Processes {DATETIMELOCAL:format} tokens in the format pattern.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#date-and-time
        /// {DATETIMELOCAL:format} generates the current local date/time using the specified format string.
        /// Note: In tests, this uses UTC time for consistency.
        /// </summary>
        private string ProcessDateTimeLocal(string pattern)
        {
            var dateTimeRegex = new Regex(@"\{DATETIMELOCAL:([^\}]+)\}", RegexOptions.IgnoreCase);
            
            return dateTimeRegex.Replace(pattern, match =>
            {
                var format = match.Groups[1].Value;
                // Use UTC for testing consistency
                return DateTime.UtcNow.ToString(format);
            });
        }

        /// <summary>
        /// Resets the sequence counter for a specific entity and attribute.
        /// Useful for testing scenarios where sequence numbers need to be reset.
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the entity</param>
        /// <param name="attributeLogicalName">The logical name of the attribute</param>
        public void ResetSequence(string entityLogicalName, string attributeLogicalName)
        {
            var key = $"{entityLogicalName}.{attributeLogicalName}";
            _sequenceCounters.TryRemove(key, out _);
        }

        /// <summary>
        /// Sets the sequence counter to a specific value for an entity and attribute.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#seed-values
        /// In Dataverse, administrators can set the seed value for auto number sequences.
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the entity</param>
        /// <param name="attributeLogicalName">The logical name of the attribute</param>
        /// <param name="seedValue">The seed value to set (next value will be seedValue + 1)</param>
        public void SetSequenceSeed(string entityLogicalName, string attributeLogicalName, long seedValue)
        {
            var key = $"{entityLogicalName}.{attributeLogicalName}";
            _sequenceCounters.AddOrUpdate(key, seedValue, (k, v) => seedValue);
        }

        /// <summary>
        /// Gets the default auto number format for common Dataverse entities.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
        /// These are the typical patterns used in standard Dataverse entities.
        /// </summary>
        public static string GetDefaultFormatForEntity(string entityLogicalName, string attributeLogicalName)
        {
            // Standard patterns for common entities
            // Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/
            
            var entityAttributeKey = $"{entityLogicalName}.{attributeLogicalName}".ToLower();
            
            var defaultFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Sales entities
                { "invoice.invoicenumber", "INV-{SEQNUM:5}" },
                { "quote.quotenumber", "QUO-{SEQNUM:5}" },
                { "salesorder.ordernumber", "ORD-{SEQNUM:5}" },
                { "opportunity.opportunitynumber", "OPP-{SEQNUM:5}" },
                
                // Service entities  
                { "incident.ticketnumber", "CAS-{SEQNUM:5}" },
                { "contract.contractnumber", "CON-{SEQNUM:5}" },
                
                // Activity entities
                { "campaignresponse.campaignresponsenumber", "CR-{SEQNUM:5}" },
                
                // Custom entity default
                { "default", "{SEQNUM:5}" }
            };

            if (defaultFormats.TryGetValue(entityAttributeKey, out var format))
            {
                return format;
            }

            // Return generic format for unknown entities
            return $"{entityLogicalName.ToUpper()}-{{SEQNUM:5}}";
        }
    }
}
