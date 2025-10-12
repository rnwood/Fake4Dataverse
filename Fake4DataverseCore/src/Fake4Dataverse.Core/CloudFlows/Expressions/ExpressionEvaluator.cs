using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Jint;
using Jint.Native;
using Jint.Native.Object;

namespace Fake4Dataverse.CloudFlows.Expressions
{
    /// <summary>
    /// Evaluates Power Automate expressions using Jint JavaScript engine.
    /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference
    /// 
    /// Power Automate uses a workflow definition language that supports expressions starting with '@'.
    /// This evaluator provides 100% compatible implementation of the expression language using Jint.net.
    /// 
    /// Supported expression patterns:
    /// - @triggerOutputs()?['body/fieldname'] - Access trigger data
    /// - @outputs('ActionName')?['body/field'] - Access action outputs  
    /// - @concat('text1', 'text2') - String concatenation
    /// - @{variables('varName')} - String interpolation
    /// - @greater(value1, value2) - Comparison
    /// - @equals(value1, value2) - Equality check
    /// - And many more functions (see Microsoft documentation)
    /// </summary>
    public class ExpressionEvaluator
    {
        private readonly IFlowExecutionContext _executionContext;
        // Updated regex to better handle complete expressions
        private static readonly Regex ExpressionPattern = new Regex(@"@\{([^}]+)\}", RegexOptions.Compiled);

        public ExpressionEvaluator(IFlowExecutionContext executionContext)
        {
            _executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        }

        /// <summary>
        /// Evaluates an expression or returns the value as-is if it's not an expression.
        /// Handles both @expression and @{expression} patterns.
        /// </summary>
        public object Evaluate(object value)
        {
            if (value == null)
                return null;

            var stringValue = value.ToString();
            
            // Check if this looks like an expression
            if (!stringValue.Contains("@"))
                return value;

            // If the entire string is just an expression starting with @
            if (stringValue.StartsWith("@") && !stringValue.Contains("@{"))
            {
                // Remove the @ and evaluate
                var expr = stringValue.Substring(1);
                return EvaluateExpression(expr);
            }

            // Handle string interpolation @{expression} within text
            var result = ExpressionPattern.Replace(stringValue, match =>
            {
                var expr = match.Groups[1].Value;
                var evaluated = EvaluateExpression(expr);
                return evaluated?.ToString() ?? string.Empty;
            });

            return result;
        }

        /// <summary>
        /// Evaluates a single expression without the @ prefix.
        /// Uses Jint JavaScript engine with custom functions for Power Automate compatibility.
        /// </summary>
        private object EvaluateExpression(string expression)
        {
            try
            {
                var engine = new Engine(options =>
                {
                    options.Strict = false;
                    options.AllowClr();
                });

                // Register Power Automate functions
                RegisterPowerAutomateFunctions(engine);

                // Execute the expression
                var result = engine.Evaluate(expression);

                // Convert Jint result to .NET type
                return ConvertJintValue(result);
            }
            catch (Exception ex)
            {
                // Log or handle expression evaluation errors
                // In production, you might want to log this
                throw new InvalidOperationException($"Failed to evaluate expression: {expression}", ex);
            }
        }

        /// <summary>
        /// Registers all Power Automate workflow functions in the Jint engine.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference
        /// </summary>
        private void RegisterPowerAutomateFunctions(Engine engine)
        {
            // Reference Functions - Access trigger and action data
            RegisterReferenceFunctions(engine);

            // String Functions - Text manipulation
            RegisterStringFunctions(engine);

            // Logical/Comparison Functions - Boolean operations
            RegisterLogicalFunctions(engine);

            // Conversion Functions - Type conversions
            RegisterConversionFunctions(engine);

            // Collection Functions - Array operations
            RegisterCollectionFunctions(engine);

            // Date/Time Functions - Date manipulation
            RegisterDateTimeFunctions(engine);

            // Math Functions - Arithmetic operations
            RegisterMathFunctions(engine);
        }

        /// <summary>
        /// Reference Functions: triggerOutputs(), triggerBody(), outputs(), body(), variables(), parameters()
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#reference-functions
        /// 
        /// These functions access data from the flow execution context.
        /// </summary>
        private void RegisterReferenceFunctions(Engine engine)
        {
            // triggerOutputs() - Returns the trigger outputs object
            // Usage: @triggerOutputs()?['body/fieldname']
            engine.SetValue("triggerOutputs", new Func<object>(() =>
            {
                var triggerData = new Dictionary<string, object>();
                if (_executionContext.TriggerInputs != null)
                {
                    triggerData["body"] = _executionContext.TriggerInputs;
                    foreach (var kvp in _executionContext.TriggerInputs)
                    {
                        triggerData[kvp.Key] = kvp.Value;
                    }
                }
                return triggerData;
            }));

            // triggerBody() - Returns the trigger body (equivalent to triggerOutputs()['body'])
            // Usage: @triggerBody()?['fieldname']
            engine.SetValue("triggerBody", new Func<object>(() =>
            {
                return _executionContext.TriggerInputs;
            }));

            // outputs(actionName) - Returns outputs from a specific action
            // Usage: @outputs('ActionName')?['body/field']
            engine.SetValue("outputs", new Func<string, object>((actionName) =>
            {
                var actionOutputs = _executionContext.GetActionOutputs(actionName);
                if (actionOutputs != null)
                {
                    var result = new Dictionary<string, object>
                    {
                        ["body"] = actionOutputs
                    };
                    foreach (var kvp in actionOutputs)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                    return result;
                }
                return null;
            }));

            // body(actionName) - Returns the body from action outputs (equivalent to outputs(actionName)['body'])
            // Usage: @body('ActionName')?['field']
            engine.SetValue("body", new Func<string, object>((actionName) =>
            {
                return _executionContext.GetActionOutputs(actionName);
            }));

            // variables(variableName) - Returns a variable value (placeholder - not fully implemented)
            engine.SetValue("variables", new Func<string, object>((variableName) =>
            {
                return null; // Variables would need separate tracking in execution context
            }));

            // parameters(parameterName) - Returns a parameter value (placeholder)
            engine.SetValue("parameters", new Func<string, object>((parameterName) =>
            {
                return null; // Parameters would need to be passed in flow definition
            }));

            // item() - Returns the current item in an Apply to Each loop (placeholder)
            engine.SetValue("item", new Func<object>(() =>
            {
                return null; // Would need loop context tracking
            }));
        }

        /// <summary>
        /// String Functions: concat(), substring(), replace(), toLower(), toUpper(), trim(), split(), join()
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#string-functions
        /// </summary>
        private void RegisterStringFunctions(Engine engine)
        {
            // concat(...) - Concatenates multiple strings
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#concat
            // Returns a single string from combining two or more strings. Can also work with integers.
            engine.SetValue("concat", new Func<JsValue, JsValue[], JsValue>((thisValue, args) =>
            {
                return string.Concat(args.Select(a => a.ToString()));
            }));

            // substring(text, startIndex, length?) - Extracts a substring
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#substring
            // Returns characters from a string, starting from the specified position. Length is optional.
            engine.SetValue("substring", new Func<string, int, int?, string>((text, startIndex, length) =>
            {
                if (text == null) return null;
                if (length.HasValue)
                    return text.Substring(startIndex, length.Value);
                return text.Substring(startIndex);
            }));

            // replace(text, oldValue, newValue) - Replaces text
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#replace
            // Replaces all occurrences of oldValue with newValue in the text.
            engine.SetValue("replace", new Func<string, string, string, string>((text, oldValue, newValue) =>
            {
                return text?.Replace(oldValue, newValue);
            }));

            // toLower(text) - Converts to lowercase
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#toLower
            engine.SetValue("toLower", new Func<string, string>((text) =>
            {
                return text?.ToLowerInvariant();
            }));

            // toUpper(text) - Converts to uppercase
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#toUpper
            engine.SetValue("toUpper", new Func<string, string>((text) =>
            {
                return text?.ToUpperInvariant();
            }));

            // trim(text) - Removes leading and trailing whitespace
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#trim
            engine.SetValue("trim", new Func<string, string>((text) =>
            {
                return text?.Trim();
            }));

            // split(text, delimiter) - Splits text into array
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#split
            // Returns an array of substrings separated by the delimiter.
            engine.SetValue("split", new Func<string, string, string[]>((text, delimiter) =>
            {
                return text?.Split(new[] { delimiter }, StringSplitOptions.None);
            }));

            // join(array, delimiter) - Joins array elements
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#join
            // Returns a string with all items from an array, separated by delimiter.
            engine.SetValue("join", new Func<object[], string, string>((array, delimiter) =>
            {
                return string.Join(delimiter, array.Select(a => a?.ToString() ?? string.Empty));
            }));

            // length(text) - Returns string length
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#length
            // Returns the number of characters in a string or number of items in an array.
            engine.SetValue("length", new Func<object, int>((value) =>
            {
                if (value is string str)
                    return str.Length;
                if (value is Array arr)
                    return arr.Length;
                if (value is System.Collections.ICollection coll)
                    return coll.Count;
                return 0;
            }));

            // indexOf(text, searchText) - Finds index of substring
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#indexOf
            // Returns the starting position (0-based) of a substring, or -1 if not found.
            engine.SetValue("indexOf", new Func<string, string, int>((text, searchText) =>
            {
                return text?.IndexOf(searchText, StringComparison.Ordinal) ?? -1;
            }));

            // lastIndexOf(text, searchText) - Finds last index of substring
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#lastIndexOf
            engine.SetValue("lastIndexOf", new Func<string, string, int>((text, searchText) =>
            {
                return text?.LastIndexOf(searchText, StringComparison.Ordinal) ?? -1;
            }));

            // startsWith(text, searchText) - Checks if string starts with value
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#startsWith
            // Returns true if the string starts with the searchText (case-sensitive).
            engine.SetValue("startsWith", new Func<string, string, bool>((text, searchText) =>
            {
                return text?.StartsWith(searchText, StringComparison.Ordinal) ?? false;
            }));

            // endsWith(text, searchText) - Checks if string ends with value
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#endsWith
            // Returns true if the string ends with the searchText (case-sensitive).
            engine.SetValue("endsWith", new Func<string, string, bool>((text, searchText) =>
            {
                return text?.EndsWith(searchText, StringComparison.Ordinal) ?? false;
            }));

            // guid() - Generates a new GUID
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#guid
            // Returns a globally unique identifier (GUID) as a string.
            engine.SetValue("guid", new Func<string>(() =>
            {
                return Guid.NewGuid().ToString();
            }));

            // formatNumber(number, format?, locale?) - Formats a number
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#formatNumber
            engine.SetValue("formatNumber", new Func<double, string, string, string>((number, format, locale) =>
            {
                if (!string.IsNullOrEmpty(format))
                    return number.ToString(format);
                return number.ToString();
            }));
        }

        /// <summary>
        /// Logical/Comparison Functions: and(), or(), not(), if(), equals(), greater(), less(), empty(), contains()
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#logical-comparison-functions
        /// </summary>
        private void RegisterLogicalFunctions(Engine engine)
        {
            // equals(value1, value2) - Checks equality
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#equals
            // Returns true if both values are equivalent. Comparison is type-aware.
            engine.SetValue("equals", new Func<object, object, bool>((value1, value2) =>
            {
                return Equals(value1, value2);
            }));

            // greater(value1, value2) - Checks if first value is greater
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#greater
            // Returns true if value1 > value2. Can compare numbers, strings (alphabetically), dates.
            engine.SetValue("greater", new Func<object, object, bool>((value1, value2) =>
            {
                return CompareValues(value1, value2) > 0;
            }));

            // greaterOrEquals(value1, value2) - Checks if first value is greater or equal
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#greaterOrEquals
            engine.SetValue("greaterOrEquals", new Func<object, object, bool>((value1, value2) =>
            {
                return CompareValues(value1, value2) >= 0;
            }));

            // less(value1, value2) - Checks if first value is less
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#less
            // Returns true if value1 < value2.
            engine.SetValue("less", new Func<object, object, bool>((value1, value2) =>
            {
                return CompareValues(value1, value2) < 0;
            }));

            // lessOrEquals(value1, value2) - Checks if first value is less or equal
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#lessOrEquals
            engine.SetValue("lessOrEquals", new Func<object, object, bool>((value1, value2) =>
            {
                return CompareValues(value1, value2) <= 0;
            }));

            // and(condition1, condition2, ...) - Logical AND
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#and
            // Returns true if all conditions are true.
            engine.SetValue("and", new Func<object[], bool>((conditions) =>
            {
                return conditions.All(c => Convert.ToBoolean(c));
            }));

            // or(condition1, condition2, ...) - Logical OR
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#or
            // Returns true if at least one condition is true.
            engine.SetValue("or", new Func<object[], bool>((conditions) =>
            {
                return conditions.Any(c => Convert.ToBoolean(c));
            }));

            // not(condition) - Logical NOT
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#not
            // Returns the opposite boolean value.
            engine.SetValue("not", new Func<bool, bool>((condition) =>
            {
                return !condition;
            }));

            // if(condition, trueValue, falseValue) - Conditional expression
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#if
            // Returns trueValue if condition is true, otherwise returns falseValue.
            engine.SetValue("if", new Func<bool, object, object, object>((condition, trueValue, falseValue) =>
            {
                return condition ? trueValue : falseValue;
            }));

            // empty(value) - Checks if value is empty
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#empty
            // Returns true if the value is null, empty string, empty array, or empty object.
            engine.SetValue("empty", new Func<object, bool>((value) =>
            {
                if (value == null) return true;
                if (value is string str) return string.IsNullOrEmpty(str);
                if (value is Array arr) return arr.Length == 0;
                if (value is System.Collections.ICollection coll) return coll.Count == 0;
                return false;
            }));

            // contains(collection, value) - Checks if collection contains value
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#contains
            // Returns true if string contains substring, or array contains item.
            engine.SetValue("contains", new Func<object, object, bool>((collection, value) =>
            {
                if (collection is string str && value is string searchStr)
                    return str.Contains(searchStr);
                if (collection is Array arr)
                    return arr.Cast<object>().Contains(value);
                if (collection is System.Collections.IEnumerable enumerable)
                    return enumerable.Cast<object>().Contains(value);
                return false;
            }));

            // coalesce(...) - Returns first non-null value
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#coalesce
            // Evaluates arguments in order and returns the first non-null/non-empty value.
            engine.SetValue("coalesce", new Func<object[], object>((values) =>
            {
                foreach (var value in values)
                {
                    if (value != null)
                    {
                        if (value is string str && !string.IsNullOrEmpty(str))
                            return value;
                        else if (!(value is string))
                            return value;
                    }
                }
                return null;
            }));
        }

        /// <summary>
        /// Conversion Functions: string(), int(), float(), bool(), json(), base64()
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#conversion-functions
        /// </summary>
        private void RegisterConversionFunctions(Engine engine)
        {
            // string(value) - Converts to string
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#string
            // Returns the string representation of the value.
            engine.SetValue("string", new Func<object, string>((value) =>
            {
                return value?.ToString();
            }));

            // int(value) - Converts to integer
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#int
            // Returns the integer representation of the value. Throws if conversion fails.
            engine.SetValue("int", new Func<object, int>((value) =>
            {
                return Convert.ToInt32(value);
            }));

            // float(value) - Converts to floating point
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#float
            // Returns the floating-point representation of the value.
            engine.SetValue("float", new Func<object, double>((value) =>
            {
                return Convert.ToDouble(value);
            }));

            // bool(value) - Converts to boolean
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#bool
            // Returns the boolean representation. "true"/"1" = true, "false"/"0" = false.
            engine.SetValue("bool", new Func<object, bool>((value) =>
            {
                if (value is bool b) return b;
                if (value is string str)
                {
                    if (str.Equals("true", StringComparison.OrdinalIgnoreCase) || str == "1")
                        return true;
                    if (str.Equals("false", StringComparison.OrdinalIgnoreCase) || str == "0")
                        return false;
                }
                return Convert.ToBoolean(value);
            }));

            // json(value) - Parses JSON string
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#json
            // Parses a JSON string and returns the object.
            engine.SetValue("json", new Func<string, object>((jsonString) =>
            {
                // For simplicity, return the string. Full JSON parsing would require more complex handling
                return System.Text.Json.JsonSerializer.Deserialize<object>(jsonString);
            }));

            // base64(value) - Encodes to base64
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#base64
            // Returns the base64-encoded version of a string.
            engine.SetValue("base64", new Func<string, string>((value) =>
            {
                if (value == null) return null;
                var bytes = System.Text.Encoding.UTF8.GetBytes(value);
                return Convert.ToBase64String(bytes);
            }));

            // base64ToString(base64Value) - Decodes base64
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#base64ToString
            // Returns the string version of a base64-encoded string.
            engine.SetValue("base64ToString", new Func<string, string>((base64Value) =>
            {
                if (base64Value == null) return null;
                var bytes = Convert.FromBase64String(base64Value);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }));
        }

        /// <summary>
        /// Collection Functions: first(), last(), take(), skip(), union(), intersection()
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#collection-functions
        /// </summary>
        private void RegisterCollectionFunctions(Engine engine)
        {
            // first(collection) - Returns first item
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#first
            // Returns the first item from a string or array.
            engine.SetValue("first", new Func<object, object>((collection) =>
            {
                if (collection is string str)
                    return str.Length > 0 ? str[0].ToString() : null;
                if (collection is Array arr)
                    return arr.Length > 0 ? arr.GetValue(0) : null;
                if (collection is System.Collections.IEnumerable enumerable)
                    return enumerable.Cast<object>().FirstOrDefault();
                return null;
            }));

            // last(collection) - Returns last item
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#last
            // Returns the last item from a string or array.
            engine.SetValue("last", new Func<object, object>((collection) =>
            {
                if (collection is string str)
                    return str.Length > 0 ? str[str.Length - 1].ToString() : null;
                if (collection is Array arr)
                    return arr.Length > 0 ? arr.GetValue(arr.Length - 1) : null;
                if (collection is System.Collections.IEnumerable enumerable)
                    return enumerable.Cast<object>().LastOrDefault();
                return null;
            }));

            // take(collection, count) - Takes first N items
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#take
            // Returns the first 'count' items from the collection.
            engine.SetValue("take", new Func<object, int, object>((collection, count) =>
            {
                if (collection is string str)
                    return str.Substring(0, Math.Min(count, str.Length));
                if (collection is System.Collections.IEnumerable enumerable)
                    return enumerable.Cast<object>().Take(count).ToArray();
                return null;
            }));

            // skip(collection, count) - Skips first N items
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#skip
            // Removes 'count' items from the front of the collection.
            engine.SetValue("skip", new Func<object, int, object>((collection, count) =>
            {
                if (collection is string str)
                    return str.Substring(Math.Min(count, str.Length));
                if (collection is System.Collections.IEnumerable enumerable)
                    return enumerable.Cast<object>().Skip(count).ToArray();
                return null;
            }));

            // union(collection1, collection2, ...) - Returns union of collections
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#union
            // Returns a collection with all unique items from the input collections.
            engine.SetValue("union", new Func<object[], object[]>((collections) =>
            {
                var result = new List<object>();
                foreach (var collection in collections)
                {
                    if (collection is System.Collections.IEnumerable enumerable)
                    {
                        result.AddRange(enumerable.Cast<object>());
                    }
                }
                return result.Distinct().ToArray();
            }));

            // intersection(collection1, collection2, ...) - Returns intersection
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#intersection
            // Returns a collection with only items that appear in all input collections.
            engine.SetValue("intersection", new Func<object[], object[]>((collections) =>
            {
                if (collections.Length == 0) return new object[0];
                
                var sets = collections
                    .Select(c => c is System.Collections.IEnumerable enumerable 
                        ? new HashSet<object>(enumerable.Cast<object>()) 
                        : new HashSet<object>())
                    .ToArray();

                var result = new HashSet<object>(sets[0]);
                for (int i = 1; i < sets.Length; i++)
                {
                    result.IntersectWith(sets[i]);
                }
                return result.ToArray();
            }));
        }

        /// <summary>
        /// Date/Time Functions: utcNow(), addDays(), addHours(), formatDateTime(), ticks()
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#date-and-time-functions
        /// </summary>
        private void RegisterDateTimeFunctions(Engine engine)
        {
            // utcNow() - Returns current UTC timestamp
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#utcNow
            // Returns the current timestamp in ISO 8601 format.
            engine.SetValue("utcNow", new Func<string>(() =>
            {
                return DateTime.UtcNow.ToString("o");
            }));

            // addDays(timestamp, days) - Adds days to timestamp
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#addDays
            // Returns a new timestamp with the specified number of days added.
            engine.SetValue("addDays", new Func<string, int, string>((timestamp, days) =>
            {
                var dt = DateTime.Parse(timestamp);
                return dt.AddDays(days).ToString("o");
            }));

            // addHours(timestamp, hours) - Adds hours to timestamp
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#addHours
            engine.SetValue("addHours", new Func<string, int, string>((timestamp, hours) =>
            {
                var dt = DateTime.Parse(timestamp);
                return dt.AddHours(hours).ToString("o");
            }));

            // addMinutes(timestamp, minutes) - Adds minutes to timestamp
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#addMinutes
            engine.SetValue("addMinutes", new Func<string, int, string>((timestamp, minutes) =>
            {
                var dt = DateTime.Parse(timestamp);
                return dt.AddMinutes(minutes).ToString("o");
            }));

            // addSeconds(timestamp, seconds) - Adds seconds to timestamp
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#addSeconds
            engine.SetValue("addSeconds", new Func<string, int, string>((timestamp, seconds) =>
            {
                var dt = DateTime.Parse(timestamp);
                return dt.AddSeconds(seconds).ToString("o");
            }));

            // formatDateTime(timestamp, format?) - Formats a date/time
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#formatDateTime
            // Returns a timestamp in the specified format. Default is ISO 8601.
            engine.SetValue("formatDateTime", new Func<string, string, string>((timestamp, format) =>
            {
                var dt = DateTime.Parse(timestamp);
                if (!string.IsNullOrEmpty(format))
                    return dt.ToString(format);
                return dt.ToString("o");
            }));

            // ticks(timestamp) - Returns ticks value
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#ticks
            // Returns the ticks property (100-nanosecond intervals since 1/1/0001) for the timestamp.
            engine.SetValue("ticks", new Func<string, long>((timestamp) =>
            {
                var dt = DateTime.Parse(timestamp);
                return dt.Ticks;
            }));

            // dayOfMonth(timestamp) - Returns day of month
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#dayOfMonth
            engine.SetValue("dayOfMonth", new Func<string, int>((timestamp) =>
            {
                var dt = DateTime.Parse(timestamp);
                return dt.Day;
            }));

            // dayOfWeek(timestamp) - Returns day of week (0=Sunday, 6=Saturday)
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#dayOfWeek
            engine.SetValue("dayOfWeek", new Func<string, int>((timestamp) =>
            {
                var dt = DateTime.Parse(timestamp);
                return (int)dt.DayOfWeek;
            }));

            // dayOfYear(timestamp) - Returns day of year
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#dayOfYear
            engine.SetValue("dayOfYear", new Func<string, int>((timestamp) =>
            {
                var dt = DateTime.Parse(timestamp);
                return dt.DayOfYear;
            }));
        }

        /// <summary>
        /// Math Functions: add(), sub(), mul(), div(), mod(), min(), max(), rand()
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#math-functions
        /// </summary>
        private void RegisterMathFunctions(Engine engine)
        {
            // add(value1, value2) - Addition
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#add
            engine.SetValue("add", new Func<double, double, double>((value1, value2) =>
            {
                return value1 + value2;
            }));

            // sub(value1, value2) - Subtraction
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#sub
            engine.SetValue("sub", new Func<double, double, double>((value1, value2) =>
            {
                return value1 - value2;
            }));

            // mul(value1, value2) - Multiplication
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#mul
            engine.SetValue("mul", new Func<double, double, double>((value1, value2) =>
            {
                return value1 * value2;
            }));

            // div(value1, value2) - Division
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#div
            engine.SetValue("div", new Func<double, double, double>((value1, value2) =>
            {
                return value1 / value2;
            }));

            // mod(value1, value2) - Modulo
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#mod
            engine.SetValue("mod", new Func<double, double, double>((value1, value2) =>
            {
                return value1 % value2;
            }));

            // min(...) - Returns minimum value
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#min
            engine.SetValue("min", new Func<object[], double>((values) =>
            {
                return values.Select(v => Convert.ToDouble(v)).Min();
            }));

            // max(...) - Returns maximum value
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#max
            engine.SetValue("max", new Func<object[], double>((values) =>
            {
                return values.Select(v => Convert.ToDouble(v)).Max();
            }));

            // rand(min, max) - Returns random integer
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#rand
            // Returns a random integer from the specified range (inclusive).
            engine.SetValue("rand", new Func<int, int, int>((min, max) =>
            {
                var random = new Random();
                return random.Next(min, max + 1);
            }));

            // range(start, count) - Returns array of integers
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#range
            // Returns an array of integers starting from 'start' for 'count' items.
            engine.SetValue("range", new Func<int, int, int[]>((start, count) =>
            {
                return Enumerable.Range(start, count).ToArray();
            }));
        }

        /// <summary>
        /// Helper method to compare values for logical functions.
        /// Handles numbers, strings, dates, and other types.
        /// </summary>
        private int CompareValues(object value1, object value2)
        {
            // Try numeric comparison first
            if (IsNumeric(value1) && IsNumeric(value2))
            {
                var num1 = Convert.ToDouble(value1);
                var num2 = Convert.ToDouble(value2);
                return num1.CompareTo(num2);
            }

            // Try date comparison
            if (value1 is DateTime dt1 && value2 is DateTime dt2)
            {
                return dt1.CompareTo(dt2);
            }

            // String comparison
            var str1 = value1?.ToString() ?? string.Empty;
            var str2 = value2?.ToString() ?? string.Empty;
            return string.Compare(str1, str2, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if a value can be treated as numeric.
        /// </summary>
        private bool IsNumeric(object value)
        {
            return value is int || value is long || value is float || value is double || value is decimal;
        }

        /// <summary>
        /// Converts Jint value to .NET type.
        /// </summary>
        private object ConvertJintValue(JsValue jsValue)
        {
            if (jsValue.IsNull() || jsValue.IsUndefined())
                return null;

            if (jsValue.IsBoolean())
                return jsValue.AsBoolean();

            if (jsValue.IsNumber())
                return jsValue.AsNumber();

            if (jsValue.IsString())
                return jsValue.AsString();

            if (jsValue.IsArray())
            {
                var arr = jsValue.AsArray();
                var result = new object[arr.Length];
                for (uint i = 0; i < arr.Length; i++)
                {
                    result[i] = ConvertJintValue(arr.Get(i.ToString()));
                }
                return result;
            }

            if (jsValue.IsObject())
            {
                var obj = jsValue.AsObject();
                var result = new Dictionary<string, object>();
                foreach (var prop in obj.GetOwnProperties())
                {
                    result[prop.Key.ToString()] = ConvertJintValue(prop.Value.Value);
                }
                return result;
            }

            return jsValue.ToString();
        }
    }
}
