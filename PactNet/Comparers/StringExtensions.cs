using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PactNet.Comparers
{
    public static class StringExtensions
    {
        /// <summary>
        /// If the string is not already terminated with the given character then the character is appended to the string.
        /// </summary>
        /// <param name="source">The source string value to terminate</param>
        /// <param name="character">The character to ensure the string value is terminated by</param>
        /// <returns>Null if the source string is null, or the string value terminated with the provided character value.</returns>
        public static string TerminateWith(this string source, char character)
        {
            var ending = character.ToString();
            if (source == null) return null;
            if (source.EndsWith(ending, StringComparison.InvariantCultureIgnoreCase)) return source;
            return source + ending;
        }

        public static string SafeTrim(this string source)
        {
            if (source == null) return null;
            return source.Trim();
        }

        public static string ToClassName(this string source)
        {
            if (source == null) return null;

            var buffer = new StringBuilder();

            buffer.Append(source.Substring(0, 1).ToUpper());
            int position = 1;

            int index;
            while ((index = source.IndexOf("_", position)) > -1)
            {
                buffer.Append(source.Substring(position, index - position));
                buffer.Append(source.Substring(index + 1, 1).ToUpper());
                position = index + 2;
            }

            buffer.Append(source.Substring(position));

            return buffer.ToString();
        }

        private static readonly Dictionary<string, string> PluralExceptions =
            new Dictionary<string, string>
                {
                    {"Hardware", @"Hardware"},
                    {"BmBatch", "BmBatches"},
                    {"Batch", "Batches"},
                    {"PrinterCategory", "PrinterCategories"},
                    {"ExtendedFunctionality","ExtendedFunctionalities"}
                    //{"person", @"people"}
                };

        public static string Pluralize(this string source)
        {
            if (PluralExceptions.ContainsKey(source)) return PluralExceptions[source];
            return source + "s";
        }

        public static string Quote(this string source)
        {
            return string.Format(@"""{0}""", source);
        }

        public static bool IsNullOrEmpty(this string source)
        {
            return string.IsNullOrEmpty(source);
        }

        /// <summary>
        /// Perform String.Format with the following arguments
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args">objects to pass to string.format</param>
        /// <returns></returns>
        public static string With(this string source, params object[] args)
        {
            return string.Format(source, args);
        }

        /// <summary>
        /// Split a string on a given character, return null if unsplit is null
        /// </summary>
        /// <param name="unsplit">string to split</param>
        /// <param name="lowercase">whether to convert to lowercase</param>
        /// <param name="splitChar">separator character</param>
        /// <returns>array of split strings</returns>
        public static string[] SafelySplitString(this string unsplit, bool lowercase = true, char splitChar = ',')
        {
            return unsplit == null ? null : (lowercase ? unsplit.ToLower() : unsplit).Split(splitChar);
        }

        public static object ConvertToType(this string value, Type destinationType)
        {
            try
            {
                var safeValue = (value ?? string.Empty).Trim();
                if (destinationType.IsEnum) return Enum.Parse(destinationType, safeValue, false);

                if (destinationType == typeof(DateTime))
                {
                    // For Dates Blank and null are null
                    return string.IsNullOrEmpty(safeValue)
                        ? default(DateTime)
                        : DateTime.Parse(safeValue, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
                }

                if (destinationType == typeof(DateTime?))
                {
                    // For Dates Blank and null are null
                    return string.IsNullOrEmpty(safeValue)
                        ? (DateTime?)null
                        : DateTime.Parse(safeValue, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
                }

                if (destinationType == typeof(int?))
                {
                    return string.IsNullOrEmpty(safeValue)
                        ? (int?)null
                        : int.Parse(safeValue);
                }

                return Convert.ChangeType(safeValue, destinationType, null);
            }
            catch (Exception castException)
            {
                throw new InvalidCastException("Could not convert string value to expected type", castException);
            }
        }

        public static bool IsNumber(this string packageId)
        {
            var regex = new Regex(@"^[0-9]*[0-9]$");
            return regex.IsMatch(packageId);
        }

        public static string Left(this string text, int length)
        {
            return (text.Length > length) ? text.Substring(0, length) : text;
        }

        public static string RemoveSpecialCharacters(this string originalString)
        {
            return new string(originalString
                .ToCharArray()
                .Where(ch => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'))
                .ToArray());
        }

        public static string StripEndingWhiteSpace(this string originalString)
        {
            string result = originalString;
            while (result.Length > 0 && Char.IsWhiteSpace(result.Last()))
            {
                result = result.Remove(result.Length - 1);
            }

            return result;
        }


        /// <summary>
        /// Removes all trailing occurrences of '\r' and/or '\n' characters.
        /// </summary>
        /// <param name="originalString">Original string</param>
        /// <returns>Modified string</returns>
        public static string RemoveTrailingCrLfInstances(this string originalString)
        {
            return originalString == null ? null : originalString.TrimEnd(new[] { '\r', '\n' });
        }

        /// <summary>
        /// Convert a string representing a number to its locale specific format. 
        /// Usefull for localizing the decimal seperator symbol, and other format symbols used in numbers.
        /// </summary>
        /// <param name="originalString">Original string containing the numeric value</param>
        /// <param name="format">Number format string</param>
        /// <returns>Formated number or the original string if the original string is not numeric</returns>
        public static string GetCultureSpecificFormattedNumber(this string originalString, string format)
        {
            string result = originalString;
            double value;
            if (double.TryParse(originalString, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                result = value.ToString(format, CultureInfo.CurrentCulture);

            return result;
        }

        /// <summary>
        /// Get numeric value out of a string representing a number to match the current number format.
        /// </summary>
        /// <param name="originalString">Original string containing the numeric value</param>
        /// <returns>Converted double value. Null if the conversion failed</returns>
        public static double? GetDoubleValueBasedOnCurrentCulture(this string originalString)
        {
            double value = 0;
            if (double.TryParse(originalString, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return value;

            return null;
        }
    }
}
