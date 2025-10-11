using NCalc;
using System;
using System.Linq;

namespace Fake4Dataverse.CalculatedFields
{
    /// <summary>
    /// Provides custom functions for NCalc that match Dataverse calculated field functions.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
    /// "Functions syntax - The following table contains information about the syntax for the functions provided 
    /// in the ACTION section of the calculated column."
    /// 
    /// Verified functions from Microsoft documentation:
    /// - CONCAT: Concatenates strings
    /// - DIFFINDAYS, DIFFINHOURS, DIFFINMINUTES, DIFFINMONTHS, DIFFINWEEKS, DIFFINYEARS: Calculate date differences
    /// - ADDHOURS, ADDDAYS, ADDWEEKS, ADDMONTHS, ADDYEARS: Add time intervals
    /// - SUBTRACTHOURS, SUBTRACTDAYS, SUBTRACTWEEKS, SUBTRACTMONTHS, SUBTRACTYEARS: Subtract time intervals
    /// - TRIMLEFT, TRIMRIGHT: Trim whitespace
    /// - Logical operators: AND, OR
    /// </summary>
    public static class DataverseFunctionExtensions
    {
        /// <summary>
        /// Registers all Dataverse custom functions with an NCalc Expression.
        /// This includes string manipulation, conditional logic, date functions, and more.
        /// 
        /// NCalc custom functions: https://github.com/ncalc/ncalc#extensibility
        /// </summary>
        /// <param name="expression">The NCalc expression to register functions with</param>
        public static void RegisterDataverseFunctions(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            expression.EvaluateFunction += EvaluateDataverseFunction;
        }

        /// <summary>
        /// Event handler that evaluates Dataverse-specific functions.
        /// NCalc extensibility: https://github.com/ncalc/ncalc#extensibility
        /// </summary>
        private static void EvaluateDataverseFunction(string name, FunctionArgs args)
        {
            switch (name.ToUpperInvariant())
            {
                case "CONCAT":
                    // CONCAT(string1, string2, ...) - Concatenates strings
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "CONCAT" - Combines multiple text values into a single text value
                    EvaluateConcat(args);
                    break;

                case "IF":
                    // IF(condition, value_if_true, value_if_false) - Conditional logic
                    // Common function pattern used in formulas for conditional evaluation
                    EvaluateIf(args);
                    break;

                case "ISNULL":
                    // ISNULL(value) - Check if value is null
                    // Common function for null checking in expressions
                    EvaluateIsNull(args);
                    break;

                case "DIFFINDAYS":
                    // DIFFINDAYS(start_date, end_date) - Calculate difference in days
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "DIFFINDAYS" - Returns the difference between two dates in days
                    EvaluateDateDiff(args, "day");
                    break;

                case "DIFFINHOURS":
                    // DIFFINHOURS(start_date, end_date) - Calculate difference in hours
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "DIFFINHOURS" - Returns the difference between two dates in hours
                    EvaluateDateDiff(args, "hour");
                    break;

                case "DIFFINMINUTES":
                    // DIFFINMINUTES(start_date, end_date) - Calculate difference in minutes
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "DIFFINMINUTES" - Returns the difference between two dates in minutes
                    EvaluateDateDiff(args, "minute");
                    break;

                case "DIFFINMONTHS":
                    // DIFFINMONTHS(start_date, end_date) - Calculate difference in months
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "DIFFINMONTHS" - Returns the difference between two dates in months
                    EvaluateDateDiff(args, "month");
                    break;

                case "DIFFINWEEKS":
                    // DIFFINWEEKS(start_date, end_date) - Calculate difference in weeks
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "DIFFINWEEKS" - Returns the difference between two dates in weeks
                    EvaluateDateDiff(args, "week");
                    break;

                case "DIFFINYEARS":
                    // DIFFINYEARS(start_date, end_date) - Calculate difference in years
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "DIFFINYEARS" - Returns the difference between two dates in years
                    EvaluateDateDiff(args, "year");
                    break;

                case "ADDHOURS":
                    // ADDHOURS(date, hours) - Add hours to a date
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "ADDHOURS" - Adds specified hours to a date
                    EvaluateAddTime(args, "hour");
                    break;

                case "ADDDAYS":
                    // ADDDAYS(date, days) - Add days to a date
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "ADDDAYS" - Adds specified days to a date
                    EvaluateAddTime(args, "day");
                    break;

                case "ADDWEEKS":
                    // ADDWEEKS(date, weeks) - Add weeks to a date
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "ADDWEEKS" - Adds specified weeks to a date
                    EvaluateAddTime(args, "week");
                    break;

                case "ADDMONTHS":
                    // ADDMONTHS(date, months) - Add months to a date
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "ADDMONTHS" - Adds specified months to a date
                    EvaluateAddTime(args, "month");
                    break;

                case "ADDYEARS":
                    // ADDYEARS(date, years) - Add years to a date
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "ADDYEARS" - Adds specified years to a date
                    EvaluateAddTime(args, "year");
                    break;

                case "SUBTRACTHOURS":
                    // SUBTRACTHOURS(date, hours) - Subtract hours from a date
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "SUBTRACTHOURS" - Subtracts specified hours from a date
                    EvaluateSubtractTime(args, "hour");
                    break;

                case "SUBTRACTDAYS":
                    // SUBTRACTDAYS(date, days) - Subtract days from a date
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "SUBTRACTDAYS" - Subtracts specified days from a date
                    EvaluateSubtractTime(args, "day");
                    break;

                case "SUBTRACTWEEKS":
                    // SUBTRACTWEEKS(date, weeks) - Subtract weeks from a date
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "SUBTRACTWEEKS" - Subtracts specified weeks from a date
                    EvaluateSubtractTime(args, "week");
                    break;

                case "SUBTRACTMONTHS":
                    // SUBTRACTMONTHS(date, months) - Subtract months from a date
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "SUBTRACTMONTHS" - Subtracts specified months from a date
                    EvaluateSubtractTime(args, "month");
                    break;

                case "SUBTRACTYEARS":
                    // SUBTRACTYEARS(date, years) - Subtract years from a date
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "SUBTRACTYEARS" - Subtracts specified years from a date
                    EvaluateSubtractTime(args, "year");
                    break;

                case "TRIMLEFT":
                    // TRIMLEFT(text) - Remove leading whitespace
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "TRIMLEFT" - Removes leading whitespace from a text value
                    EvaluateTrimLeft(args);
                    break;

                case "TRIMRIGHT":
                    // TRIMRIGHT(text) - Remove trailing whitespace
                    // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
                    // Verified in documentation: "TRIMRIGHT" - Removes trailing whitespace from a text value
                    EvaluateTrimRight(args);
                    break;

                // Additional common functions for compatibility
                case "UPPER":
                    EvaluateUpper(args);
                    break;

                case "LOWER":
                    EvaluateLower(args);
                    break;

                case "ROUND":
                    EvaluateRound(args);
                    break;

                case "ABS":
                    EvaluateAbs(args);
                    break;

                case "FLOOR":
                    EvaluateFloor(args);
                    break;

                case "CEILING":
                    EvaluateCeiling(args);
                    break;

                case "LEN":
                    EvaluateLen(args);
                    break;

                case "TRIM":
                    EvaluateTrim(args);
                    break;

                case "REPLACE":
                case "SUBSTITUTE":
                    EvaluateReplace(args);
                    break;

                case "LEFT":
                    EvaluateLeft(args);
                    break;

                case "RIGHT":
                    EvaluateRight(args);
                    break;

                case "MID":
                    EvaluateMid(args);
                    break;

                case "NOW":
                    EvaluateNow(args);
                    break;

                case "TODAY":
                    EvaluateToday(args);
                    break;
            }
        }

        private static void EvaluateConcat(FunctionArgs args)
        {
            var parts = args.Parameters.Select(p => p.Evaluate()?.ToString() ?? string.Empty).ToArray();
            args.Result = string.Concat(parts);
        }

        private static void EvaluateIf(FunctionArgs args)
        {
            if (args.Parameters.Length != 3)
                throw new ArgumentException("IF function requires 3 parameters: condition, value_if_true, value_if_false");

            var condition = Convert.ToBoolean(args.Parameters[0].Evaluate());
            args.Result = condition ? args.Parameters[1].Evaluate() : args.Parameters[2].Evaluate();
        }

        private static void EvaluateIsNull(FunctionArgs args)
        {
            if (args.Parameters.Length != 1)
                throw new ArgumentException("ISNULL function requires 1 parameter");

            args.Result = args.Parameters[0].Evaluate() == null;
        }

        private static void EvaluateDateDiff(FunctionArgs args, string unit)
        {
            if (args.Parameters.Length != 2)
                throw new ArgumentException($"DIFFIN{unit.ToUpper()}S function requires 2 parameters: start_date, end_date");

            var startDate = Convert.ToDateTime(args.Parameters[0].Evaluate());
            var endDate = Convert.ToDateTime(args.Parameters[1].Evaluate());

            TimeSpan diff = endDate - startDate;

            switch (unit)
            {
                case "day":
                    args.Result = (int)diff.TotalDays;
                    break;
                case "hour":
                    args.Result = (int)diff.TotalHours;
                    break;
                case "minute":
                    args.Result = (int)diff.TotalMinutes;
                    break;
                case "week":
                    args.Result = (int)(diff.TotalDays / 7);
                    break;
                case "month":
                    args.Result = ((endDate.Year - startDate.Year) * 12) + (endDate.Month - startDate.Month);
                    break;
                case "year":
                    args.Result = endDate.Year - startDate.Year;
                    break;
                default:
                    throw new ArgumentException($"Unsupported date difference unit: {unit}");
            }
        }

        private static void EvaluateAddTime(FunctionArgs args, string unit)
        {
            if (args.Parameters.Length != 2)
                throw new ArgumentException($"ADD{unit.ToUpper()}S function requires 2 parameters: date, value");

            var date = Convert.ToDateTime(args.Parameters[0].Evaluate());
            var value = Convert.ToInt32(args.Parameters[1].Evaluate());

            switch (unit)
            {
                case "hour":
                    args.Result = date.AddHours(value);
                    break;
                case "day":
                    args.Result = date.AddDays(value);
                    break;
                case "week":
                    args.Result = date.AddDays(value * 7);
                    break;
                case "month":
                    args.Result = date.AddMonths(value);
                    break;
                case "year":
                    args.Result = date.AddYears(value);
                    break;
                default:
                    throw new ArgumentException($"Unsupported time unit: {unit}");
            }
        }

        private static void EvaluateSubtractTime(FunctionArgs args, string unit)
        {
            if (args.Parameters.Length != 2)
                throw new ArgumentException($"SUBTRACT{unit.ToUpper()}S function requires 2 parameters: date, value");

            var date = Convert.ToDateTime(args.Parameters[0].Evaluate());
            var value = Convert.ToInt32(args.Parameters[1].Evaluate());

            switch (unit)
            {
                case "hour":
                    args.Result = date.AddHours(-value);
                    break;
                case "day":
                    args.Result = date.AddDays(-value);
                    break;
                case "week":
                    args.Result = date.AddDays(-value * 7);
                    break;
                case "month":
                    args.Result = date.AddMonths(-value);
                    break;
                case "year":
                    args.Result = date.AddYears(-value);
                    break;
                default:
                    throw new ArgumentException($"Unsupported time unit: {unit}");
            }
        }

        private static void EvaluateTrimLeft(FunctionArgs args)
        {
            if (args.Parameters.Length != 1)
                throw new ArgumentException("TRIMLEFT function requires 1 parameter");

            var value = args.Parameters[0].Evaluate()?.ToString();
            args.Result = value?.TrimStart();
        }

        private static void EvaluateTrimRight(FunctionArgs args)
        {
            if (args.Parameters.Length != 1)
                throw new ArgumentException("TRIMRIGHT function requires 1 parameter");

            var value = args.Parameters[0].Evaluate()?.ToString();
            args.Result = value?.TrimEnd();
        }

        private static void EvaluateUpper(FunctionArgs args)
        {
            if (args.Parameters.Length != 1)
                throw new ArgumentException("UPPER function requires 1 parameter");

            var value = args.Parameters[0].Evaluate()?.ToString();
            args.Result = value?.ToUpperInvariant();
        }

        private static void EvaluateLower(FunctionArgs args)
        {
            if (args.Parameters.Length != 1)
                throw new ArgumentException("LOWER function requires 1 parameter");

            var value = args.Parameters[0].Evaluate()?.ToString();
            args.Result = value?.ToLowerInvariant();
        }

        private static void EvaluateRound(FunctionArgs args)
        {
            if (args.Parameters.Length != 2)
                throw new ArgumentException("ROUND function requires 2 parameters: number, decimals");

            var number = Convert.ToDecimal(args.Parameters[0].Evaluate());
            var decimals = Convert.ToInt32(args.Parameters[1].Evaluate());
            args.Result = Math.Round(number, decimals);
        }

        private static void EvaluateAbs(FunctionArgs args)
        {
            if (args.Parameters.Length != 1)
                throw new ArgumentException("ABS function requires 1 parameter");

            var value = args.Parameters[0].Evaluate();
            if (value is decimal d)
                args.Result = Math.Abs(d);
            else if (value is double db)
                args.Result = Math.Abs(db);
            else if (value is int i)
                args.Result = Math.Abs(i);
            else if (value is long l)
                args.Result = Math.Abs(l);
            else
                args.Result = Math.Abs(Convert.ToDecimal(value));
        }

        private static void EvaluateFloor(FunctionArgs args)
        {
            if (args.Parameters.Length != 1)
                throw new ArgumentException("FLOOR function requires 1 parameter");

            var value = Convert.ToDecimal(args.Parameters[0].Evaluate());
            args.Result = Math.Floor(value);
        }

        private static void EvaluateCeiling(FunctionArgs args)
        {
            if (args.Parameters.Length != 1)
                throw new ArgumentException("CEILING function requires 1 parameter");

            var value = Convert.ToDecimal(args.Parameters[0].Evaluate());
            args.Result = Math.Ceiling(value);
        }

        private static void EvaluateLen(FunctionArgs args)
        {
            if (args.Parameters.Length != 1)
                throw new ArgumentException("LEN function requires 1 parameter");

            var value = args.Parameters[0].Evaluate()?.ToString();
            args.Result = value?.Length ?? 0;
        }

        private static void EvaluateTrim(FunctionArgs args)
        {
            if (args.Parameters.Length != 1)
                throw new ArgumentException("TRIM function requires 1 parameter");

            var value = args.Parameters[0].Evaluate()?.ToString();
            args.Result = value?.Trim();
        }

        private static void EvaluateReplace(FunctionArgs args)
        {
            if (args.Parameters.Length != 3)
                throw new ArgumentException("REPLACE function requires 3 parameters: text, old_text, new_text");

            var text = args.Parameters[0].Evaluate()?.ToString() ?? string.Empty;
            var oldText = args.Parameters[1].Evaluate()?.ToString() ?? string.Empty;
            var newText = args.Parameters[2].Evaluate()?.ToString() ?? string.Empty;
            args.Result = text.Replace(oldText, newText);
        }

        private static void EvaluateLeft(FunctionArgs args)
        {
            if (args.Parameters.Length != 2)
                throw new ArgumentException("LEFT function requires 2 parameters: text, length");

            var text = args.Parameters[0].Evaluate()?.ToString() ?? string.Empty;
            var length = Convert.ToInt32(args.Parameters[1].Evaluate());
            args.Result = length >= text.Length ? text : text.Substring(0, length);
        }

        private static void EvaluateRight(FunctionArgs args)
        {
            if (args.Parameters.Length != 2)
                throw new ArgumentException("RIGHT function requires 2 parameters: text, length");

            var text = args.Parameters[0].Evaluate()?.ToString() ?? string.Empty;
            var length = Convert.ToInt32(args.Parameters[1].Evaluate());
            args.Result = length >= text.Length ? text : text.Substring(text.Length - length);
        }

        private static void EvaluateMid(FunctionArgs args)
        {
            if (args.Parameters.Length != 3)
                throw new ArgumentException("MID function requires 3 parameters: text, start, length");

            var text = args.Parameters[0].Evaluate()?.ToString() ?? string.Empty;
            var start = Convert.ToInt32(args.Parameters[1].Evaluate()) - 1; // 1-based indexing
            var length = Convert.ToInt32(args.Parameters[2].Evaluate());

            if (start < 0 || start >= text.Length)
            {
                args.Result = string.Empty;
                return;
            }

            var remainingLength = text.Length - start;
            var actualLength = Math.Min(length, remainingLength);
            args.Result = text.Substring(start, actualLength);
        }

        private static void EvaluateNow(FunctionArgs args)
        {
            args.Result = DateTime.UtcNow;
        }

        private static void EvaluateToday(FunctionArgs args)
        {
            args.Result = DateTime.UtcNow.Date;
        }
    }
}
