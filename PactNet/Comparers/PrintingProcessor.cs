using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PactNet.Comparers
{
    public class PrintingProcessor : IObjectMatcherProcessor<string>
    {
        private static string PrintLocation(string hierarchy)
        {
            if (string.IsNullOrEmpty(hierarchy))
                return string.Empty;
            return hierarchy + ":";
        }

        public virtual string MismatchObject(object expected, object actual, string hierarchy)
        {
            if (expected == null && actual != null)
                return "{0} expected null but found {1}".With(PrintLocation(hierarchy), actual).Trim();
            if (expected != null && actual == null)
                return "{0} expected {1} but found null".With(PrintLocation(hierarchy), expected).Trim();
            return "{0} expected {1} but found {2}".With(PrintLocation(hierarchy), expected, actual).Trim();
        }

        public virtual string MismatchObjectReference(object expected, object actual, string hierarchy)
        {
            return "{0} expected reference to {1} but found reference to {2}".With(PrintLocation(hierarchy), expected, actual).Trim();
        }

        public virtual string MismatchCollectionLength(int expected, int actual, ICollection expectedCollection, ICollection actualCollection, string hierarchy)
        {
            return "{0} expected collection length {1} but found length {2}".With(PrintLocation(hierarchy), expected, actual).Trim();
        }

        public virtual string MisMatchEnumerableLength(int expected, int actual, IEnumerable expectedEnumerable, IEnumerable actualEnumerable, string hierarchy)
        {
            if (expected < actual)
            {
                return "{0} expected collection length {1} but found at least {2}".With(PrintLocation(hierarchy), expected, actual);
            }
            return "{0} expected collection length at least {1} but only found {2}".With(PrintLocation(hierarchy), expected, actual);
        }

        public virtual string MismatchDictionaryKeys(ICollection expectedKeys, ICollection actualKeys, IDictionary expectedDictionary, IDictionary actualDictionary, string hierarchy)
        {
            string expectedKeysString = string.Join(",", expectedKeys.Cast<object>().Select(o => o.ToString()));
            string actualKeysString = string.Join(",", actualKeys.Cast<object>().Select(o => o.ToString()));
            return "{0} unexpected dictionary keys\nExpected: [{1}]\nActual: [{2}]".With(PrintLocation(hierarchy),
                expectedKeysString, actualKeysString).Trim();
        }

        public virtual bool ShouldCompareProperty(object expected, object actual, PropertyInfo property)
        {
            return true;
        }

        public virtual bool ShouldCompareField(object expected, object actual, FieldInfo field)
        {
            return true;
        }

        public string DefaultValue
        {
            get { return default(string); }
        }
    }
}
