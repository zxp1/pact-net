using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PactNet.Comparers
{
    public class BoolProcessor : IObjectMatcherProcessor<bool>
    {
        public bool MismatchObject(object expected, object actual, string hierarchy)
        {
            return false;
        }

        public bool MismatchObjectReference(object expected, object actual, string hierarchy)
        {
            return false;
        }

        public bool MismatchCollectionLength(int expected, int actual, ICollection expectedCollection, ICollection actualCollection, string hierarchy)
        {
            return false;
        }

        public bool MisMatchEnumerableLength(int expected, int actual, IEnumerable expectedEnumerable, IEnumerable actualEnumerable, string hierarchy)
        {
            return false;
        }

        public bool MismatchDictionaryKeys(ICollection expectedKeys, ICollection actualKeys, IDictionary expectedDictionary, IDictionary actualDictionary, string hierarchy)
        {
            return false;
        }

        public bool ShouldCompareProperty(object expected, object actual, PropertyInfo property)
        {
            return true;
        }

        public bool ShouldCompareField(object expected, object actual, FieldInfo field)
        {
            return true;
        }

        public bool DefaultValue
        {
            get { return true; }
        }
    }
}
