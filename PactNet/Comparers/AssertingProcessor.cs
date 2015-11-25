using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PactNet.Comparers
{
    public class AssertingProcessor : PrintingProcessor
    {
        public override string MismatchObject(object expected, object actual, string hierarchy)
        {
            var result = base.MismatchObject(expected, actual, hierarchy);
            //Assert.Fail(result);
            return result;
        }

        public override string MismatchObjectReference(object expected, object actual, string hierarchy)
        {
            var result = base.MismatchObjectReference(expected, actual, hierarchy);
            //Assert.Fail(result);
            return result;
        }

        public override string MismatchCollectionLength(int expected, int actual, ICollection expectedCollection, ICollection actualCollection, string hierarchy)
        {
            var result = base.MismatchCollectionLength(expected, actual, expectedCollection, actualCollection, hierarchy);
           // Assert.Fail(result);
            return result;
        }

        public override string MismatchDictionaryKeys(ICollection expectedKeys, ICollection actualKeys, IDictionary expectedDictionary, IDictionary actualDictionary, string hierarchy)
        {
            var result = base.MismatchDictionaryKeys(expectedKeys, actualKeys, expectedDictionary, actualDictionary, hierarchy);
            //Assert.Fail(result);
            return result;
        }

        public override string MisMatchEnumerableLength(int expected, int actual, IEnumerable expectedEnumerable, IEnumerable actualEnumerable, string hierarchy)
        {
            var result = base.MisMatchEnumerableLength(expected, actual, expectedEnumerable, actualEnumerable, hierarchy);
            //Assert.Fail(result);
            return result;
        }
    }
}
