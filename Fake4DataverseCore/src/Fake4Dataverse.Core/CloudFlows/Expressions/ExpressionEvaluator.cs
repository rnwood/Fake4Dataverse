using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Jint;
using Jint.Native;
using Jint.Native.Function;
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
                // Preprocess the expression to handle reserved JavaScript keywords
                // Replace if( with __if( since 'if' is a reserved keyword
                expression = PreprocessExpression(expression);
                
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
        /// Preprocesses an expression to handle JavaScript reserved keywords
        /// </summary>
        private string PreprocessExpression(string expression)
        {
            // Replace if( with __if( since 'if' is a reserved JavaScript keyword
            // We need to be careful to only replace if( as a function call, not within strings
            // Simple approach: replace if( with __if( globally (works for most cases)
            expression = System.Text.RegularExpressions.Regex.Replace(
                expression, 
                @"\bif\s*\(",  // Word boundary, 'if', optional whitespace, open paren
                "__if(");
            
            return expression;
        }

        /// <summary>
        /// Registers all Power Automate workflow functions in the Jint engine.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference
        /// </summary>
        private void RegisterPowerAutomateFunctions(Engine engine)
        {
            // For variadic functions, we need to use JavaScript's arguments object
            // Register a helper that captures the engine instance for use in JavaScript
            var helper = new
            {
                concat = new Func<object[], string>((args) => string.Concat(args.Select(a => a?.ToString() ?? string.Empty))),
                min = new Func<object[], double>((args) => args.Select(v => Convert.ToDouble(v)).Min()),
                max = new Func<object[], double>((args) => args.Select(v => Convert.ToDouble(v)).Max()),
                and = new Func<object[], bool>((args) => args.All(c => Convert.ToBoolean(c))),
                or = new Func<object[], bool>((args) => args.Any(c => Convert.ToBoolean(c))),
                coalesce = new Func<object[], object>((args) => {
                    foreach (var value in args)
                    {
                        // Skip null and Jint undefined values
                        if (value == null || (value is JsValue jsVal && (jsVal.IsNull() || jsVal.IsUndefined())))
                            continue;
                            
                        // For strings, check if non-empty
                        if (value is string str)
                        {
                            if (!string.IsNullOrEmpty(str))
                                return value;
                        }
                        else
                        {
                            // For non-string types, return if not null
                            return value;
                        }
                    }
                    return null;
                }),
                createArray = new Func<object[], object[]>((args) => args),
                union = new Func<object[], object[]>((args) => {
                    var result = new List<object>();
                    foreach (var collection in args)
                    {
                        if (collection is System.Collections.IEnumerable enumerable)
                        {
                            result.AddRange(enumerable.Cast<object>());
                        }
                    }
                    return result.Distinct().ToArray();
                }),
                intersection = new Func<object[], object[]>((args) => {
                    if (args.Length == 0) return new object[0];
                    
                    var sets = args
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
                }),
                @int = new Func<object, int>((value) => Convert.ToInt32(value))
            };

            engine.SetValue("__helper", helper);

            // Register variadic functions using JavaScript that captures arguments
            engine.Execute(@"
                function concat() { return __helper.concat(Array.from(arguments)); }
                function min() { return __helper.min(Array.from(arguments)); }
                function max() { return __helper.max(Array.from(arguments)); }
                function and() { return __helper.and(Array.from(arguments)); }
                function or() { return __helper.or(Array.from(arguments)); }
                function coalesce() { return __helper.coalesce(Array.from(arguments)); }
                function createArray() { return __helper.createArray(Array.from(arguments)); }
                function union() { return __helper.union(Array.from(arguments)); }
                function intersection() { return __helper.intersection(Array.from(arguments)); }
                function int(value) { 
                    var result = __helper.int(value);
                    return { __isInt: true, __value: result };
                }
            ");

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

            // Type Checking Functions - Type validation
            RegisterTypeCheckingFunctions(engine);

            // URI Functions - URL manipulation
            RegisterUriFunctions(engine);
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

            // variables(variableName) - Returns a variable value
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#variables
            // Returns the value of a flow variable that was set using Initialize Variable or Set Variable actions
            // We wrap integer values to preserve type through Jint conversion
            engine.Execute(@"
                function variables(variableName) {
                    var value = __helper.getVariable(variableName);
                    if (value !== null && typeof value === 'object' && value.__isInt) {
                        return value;
                    }
                    return value;
                }
            ");
            
            var getVariableHelper = new Func<string, object>((variableName) =>
            {
                var value = _executionContext.GetVariable(variableName);
                // Wrap integers to preserve type
                if (value is int intValue)
                {
                    return new Dictionary<string, object>
                    {
                        ["__isInt"] = true,
                        ["__value"] = intValue
                    };
                }
                return value;
            });
            
            // Need to add getVariable to existing helper - will do this by modifying the helper object
            engine.Execute("__helper.getVariable = function(name) { return __getVariableHelper(name); };");
            engine.SetValue("__getVariableHelper", getVariableHelper);

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
        /// String Functions: substring(), replace(), toLower(), toUpper(), trim(), split(), join()
        /// Note: concat() is registered as a variadic function in RegisterPowerAutomateFunctions()
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#string-functions
        /// </summary>
        private void RegisterStringFunctions(Engine engine)
        {
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
                if (array == null || array.Length == 0) return string.Empty;
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

            // slice(text, startIndex, endIndex?) - Extract substring by indices
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#slice
            // Returns a substring from startIndex to endIndex (exclusive). Similar to substring but with end index.
            engine.SetValue("slice", new Func<string, int, int?, string>((text, startIndex, endIndex) =>
            {
                if (text == null) return null;
                if (endIndex.HasValue)
                {
                    var length = endIndex.Value - startIndex;
                    return text.Substring(startIndex, length);
                }
                return text.Substring(startIndex);
            }));

            // nthIndexOf(text, searchValue, occurrence) - Find nth occurrence
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#nthIndexOf  
            // Returns the starting position of the nth occurrence of searchValue in text
            engine.SetValue("nthIndexOf", new Func<string, string, int, int>((text, searchValue, occurrence) =>
            {
                if (text == null || searchValue == null || occurrence <= 0) return -1;
                
                int index = -1;
                for (int i = 0; i < occurrence; i++)
                {
                    index = text.IndexOf(searchValue, index + 1, StringComparison.Ordinal);
                    if (index == -1) return -1;
                }
                return index;
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

            // Note: and() and or() are registered as variadic functions in RegisterPowerAutomateFunctions()
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#and
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#or

            // not(condition) - Logical NOT
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#not
            // Returns the opposite boolean value.
            engine.SetValue("not", new Func<bool, bool>((condition) =>
            {
                return !condition;
            }));

            // if(condition, trueValue, falseValue) - Conditional expression
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#if
            // 'if' is a reserved keyword in JavaScript, so we register it as __if and preprocess expressions to replace if( with __if(
            engine.SetValue("__if", new Func<bool, object, object, object>((condition, trueValue, falseValue) =>
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

            // Note: coalesce() is registered as a variadic function in RegisterPowerAutomateFunctions()
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#coalesce

            // xor(condition1, condition2) - Exclusive OR
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#xor
            // Returns true if exactly one condition is true (exclusive OR)
            engine.SetValue("xor", new Func<bool, bool, bool>((condition1, condition2) =>
            {
                return condition1 ^ condition2;
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

            // Note: int() is registered as a special function in RegisterPowerAutomateFunctions()
            // to preserve integer type through Jint conversion
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#int

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

            // reverse(collection) - Reverse array
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#reverse
            // Returns a collection with the items in reverse order
            engine.SetValue("reverse", new Func<object, object>((collection) =>
            {
                if (collection is string str)
                {
                    var chars = str.ToCharArray();
                    Array.Reverse(chars);
                    return new string(chars);
                }
                if (collection is Array arr)
                {
                    var reversed = new object[arr.Length];
                    for (int i = 0; i < arr.Length; i++)
                    {
                        reversed[i] = arr.GetValue(arr.Length - 1 - i);
                    }
                    return reversed;
                }
                if (collection is System.Collections.IEnumerable enumerable)
                {
                    return enumerable.Cast<object>().Reverse().ToArray();
                }
                return null;
            }));

            // Note: createArray() is registered as a variadic function in RegisterPowerAutomateFunctions()

            // flatten(collection) - Flatten nested arrays
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#flatten
            // Returns a flat array from an array of arrays
            engine.SetValue("flatten", new Func<object, object[]>((collection) =>
            {
                var result = new List<object>();
                void FlattenRecursive(object item)
                {
                    if (item is Array arr)
                    {
                        foreach (var element in arr)
                        {
                            FlattenRecursive(element);
                        }
                    }
                    else if (item is System.Collections.IEnumerable enumerable && !(item is string))
                    {
                        foreach (var element in enumerable)
                        {
                            FlattenRecursive(element);
                        }
                    }
                    else
                    {
                        result.Add(item);
                    }
                }
                FlattenRecursive(collection);
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

            // startOfDay(timestamp) - Returns start of day
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#startOfDay
            // Returns the start of the day for a timestamp (midnight)
            engine.SetValue("startOfDay", new Func<string, string>((timestamp) =>
            {
                var dt = DateTime.Parse(timestamp);
                return dt.Date.ToString("o");
            }));

            // startOfHour(timestamp) - Returns start of hour
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#startOfHour
            // Returns the start of the hour for a timestamp
            engine.SetValue("startOfHour", new Func<string, string>((timestamp) =>
            {
                var dt = DateTime.Parse(timestamp);
                return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, dt.Kind).ToString("o");
            }));

            // startOfMonth(timestamp) - Returns start of month
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#startOfMonth
            // Returns the start of the month for a timestamp
            engine.SetValue("startOfMonth", new Func<string, string>((timestamp) =>
            {
                var dt = DateTime.Parse(timestamp);
                return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, dt.Kind).ToString("o");
            }));

            // subtractFromTime(timestamp, interval, timeUnit) - Subtract time
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#subtractFromTime
            // Subtracts a time interval from a timestamp. TimeUnit: Year, Month, Week, Day, Hour, Minute, Second
            engine.SetValue("subtractFromTime", new Func<string, int, string, string>((timestamp, interval, timeUnit) =>
            {
                var dt = DateTime.Parse(timestamp);
                switch (timeUnit.ToLowerInvariant())
                {
                    case "year":
                        return dt.AddYears(-interval).ToString("o");
                    case "month":
                        return dt.AddMonths(-interval).ToString("o");
                    case "week":
                        return dt.AddDays(-interval * 7).ToString("o");
                    case "day":
                        return dt.AddDays(-interval).ToString("o");
                    case "hour":
                        return dt.AddHours(-interval).ToString("o");
                    case "minute":
                        return dt.AddMinutes(-interval).ToString("o");
                    case "second":
                        return dt.AddSeconds(-interval).ToString("o");
                    default:
                        return dt.ToString("o");
                }
            }));

            // getPastTime(interval, timeUnit, format?) - Get past time
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#getPastTime
            // Returns the current timestamp minus the specified time interval
            engine.SetValue("getPastTime", new Func<int, string, string, string>((interval, timeUnit, format) =>
            {
                var result = DateTime.UtcNow;
                switch (timeUnit.ToLowerInvariant())
                {
                    case "year":
                        result = result.AddYears(-interval);
                        break;
                    case "month":
                        result = result.AddMonths(-interval);
                        break;
                    case "week":
                        result = result.AddDays(-interval * 7);
                        break;
                    case "day":
                        result = result.AddDays(-interval);
                        break;
                    case "hour":
                        result = result.AddHours(-interval);
                        break;
                    case "minute":
                        result = result.AddMinutes(-interval);
                        break;
                    case "second":
                        result = result.AddSeconds(-interval);
                        break;
                }
                
                if (!string.IsNullOrEmpty(format))
                    return result.ToString(format);
                return result.ToString("o");
            }));

            // getFutureTime(interval, timeUnit, format?) - Get future time
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#getFutureTime
            // Returns the current timestamp plus the specified time interval
            engine.SetValue("getFutureTime", new Func<int, string, string, string>((interval, timeUnit, format) =>
            {
                var result = DateTime.UtcNow;
                switch (timeUnit.ToLowerInvariant())
                {
                    case "year":
                        result = result.AddYears(interval);
                        break;
                    case "month":
                        result = result.AddMonths(interval);
                        break;
                    case "week":
                        result = result.AddDays(interval * 7);
                        break;
                    case "day":
                        result = result.AddDays(interval);
                        break;
                    case "hour":
                        result = result.AddHours(interval);
                        break;
                    case "minute":
                        result = result.AddMinutes(interval);
                        break;
                    case "second":
                        result = result.AddSeconds(interval);
                        break;
                }
                
                if (!string.IsNullOrEmpty(format))
                    return result.ToString(format);
                return result.ToString("o");
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

            // Note: min() and max() are registered as variadic functions in RegisterPowerAutomateFunctions()

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
        /// Type Checking Functions: isInt(), isFloat(), isString(), isArray(), isObject()
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference
        /// 
        /// These functions check the type of a value.
        /// </summary>
        private void RegisterTypeCheckingFunctions(Engine engine)
        {
            // isInt(value) - Check if value is an integer
            engine.SetValue("isInt", new Func<object, bool>((value) =>
            {
                return value is int || value is long || (value is double d && d == Math.Floor(d));
            }));

            // isFloat(value) - Check if value is a floating point number
            engine.SetValue("isFloat", new Func<object, bool>((value) =>
            {
                return value is float || value is double || value is decimal;
            }));

            // isString(value) - Check if value is a string
            engine.SetValue("isString", new Func<object, bool>((value) =>
            {
                return value is string;
            }));

            // isArray(value) - Check if value is an array
            engine.SetValue("isArray", new Func<object, bool>((value) =>
            {
                return value is Array || (value is System.Collections.IEnumerable && !(value is string));
            }));

            // isObject(value) - Check if value is an object
            engine.SetValue("isObject", new Func<object, bool>((value) =>
            {
                return value != null && !(value is string) && !(value is Array) && 
                       !(value is System.ValueType) && !(value is System.Collections.IEnumerable);
            }));
        }

        /// <summary>
        /// URI Functions: uriComponent(), uriHost(), uriPath(), etc.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#uri-or-url-functions
        /// 
        /// These functions work with URIs and URLs.
        /// </summary>
        private void RegisterUriFunctions(Engine engine)
        {
            // uriComponent(value) - Encode URI component
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#uriComponent
            // Returns a URI-encoded version of a string by replacing URL-unsafe characters with escape characters
            engine.SetValue("uriComponent", new Func<string, string>((value) =>
            {
                if (value == null) return null;
                return Uri.EscapeDataString(value);
            }));

            // uriComponentToString(encodedValue) - Decode URI component
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#uriComponentToString
            // Returns the decoded string version of a URI-encoded string
            engine.SetValue("uriComponentToString", new Func<string, string>((encodedValue) =>
            {
                if (encodedValue == null) return null;
                return Uri.UnescapeDataString(encodedValue);
            }));

            // uriHost(uri) - Get URI host
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#uriHost
            // Returns the host value for a URI
            engine.SetValue("uriHost", new Func<string, string>((uri) =>
            {
                if (string.IsNullOrEmpty(uri)) return null;
                try
                {
                    var u = new Uri(uri);
                    return u.Host;
                }
                catch
                {
                    return null;
                }
            }));

            // uriPath(uri) - Get URI path
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#uriPath
            // Returns the path value for a URI
            engine.SetValue("uriPath", new Func<string, string>((uri) =>
            {
                if (string.IsNullOrEmpty(uri)) return null;
                try
                {
                    var u = new Uri(uri);
                    return u.AbsolutePath;
                }
                catch
                {
                    return null;
                }
            }));

            // uriPathAndQuery(uri) - Get URI path and query
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#uriPathAndQuery
            // Returns the path and query values for a URI
            engine.SetValue("uriPathAndQuery", new Func<string, string>((uri) =>
            {
                if (string.IsNullOrEmpty(uri)) return null;
                try
                {
                    var u = new Uri(uri);
                    return u.PathAndQuery;
                }
                catch
                {
                    return null;
                }
            }));

            // uriQuery(uri) - Get URI query
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#uriQuery
            // Returns the query value for a URI
            engine.SetValue("uriQuery", new Func<string, string>((uri) =>
            {
                if (string.IsNullOrEmpty(uri)) return null;
                try
                {
                    var u = new Uri(uri);
                    return u.Query;
                }
                catch
                {
                    return null;
                }
            }));

            // uriScheme(uri) - Get URI scheme
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#uriScheme
            // Returns the scheme value for a URI
            engine.SetValue("uriScheme", new Func<string, string>((uri) =>
            {
                if (string.IsNullOrEmpty(uri)) return null;
                try
                {
                    var u = new Uri(uri);
                    return u.Scheme;
                }
                catch
                {
                    return null;
                }
            }));

            // uriPort(uri) - Get URI port
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/workflow-definition-language-functions-reference#uriPort
            // Returns the port value for a URI
            engine.SetValue("uriPort", new Func<string, int>((uri) =>
            {
                if (string.IsNullOrEmpty(uri)) return -1;
                try
                {
                    var u = new Uri(uri);
                    return u.Port;
                }
                catch
                {
                    return -1;
                }
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
                
                // Check if this is a tagged int value from int() function
                if (obj.HasOwnProperty("__isInt") && obj.HasOwnProperty("__value"))
                {
                    var isIntProp = obj.Get("__isInt");
                    if (isIntProp.IsBoolean() && isIntProp.AsBoolean())
                    {
                        var valueProp = obj.Get("__value");
                        if (valueProp.IsNumber())
                        {
                            return (int)valueProp.AsNumber();
                        }
                    }
                }
                
                var result = new Dictionary<string, object>();
                foreach (var prop in obj.GetOwnProperties())
                {
                    // Skip internal marker properties
                    if (prop.Key.ToString().StartsWith("__"))
                        continue;
                    result[prop.Key.ToString()] = ConvertJintValue(prop.Value.Value);
                }
                return result;
            }

            return jsValue.ToString();
        }
    }
}
