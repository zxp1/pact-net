using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PactNet.Comparers
{
    internal class TestUtils
    {
        public static bool CheckAllPropertiesAreEqual(
            object expected,
            object actual,
            IEnumerable<string> ignoreList = null,
            bool ignoreDateTimeMilliseconds = false)
        {
            return AssertingObjectComparer.CompareValues(expected, actual, AssertingObjectComparer.BoolResult, true,
                ignoreList, ignoreDateTimeMilliseconds);
        }

        public class ObjectComparer
        {
            public static readonly PrintingProcessor StringResult = new PrintingProcessor();
            public static readonly BoolProcessor BoolResult = new BoolProcessor();

            public static TResult CompareValues<TResult>(object expected, object actual,
                IObjectMatcherProcessor<TResult> processor, bool enforceReferences,
                IEnumerable<string> ignoreList = null, bool ignoreDateTimeMilliseconds = false)
            {
                return
                    new ObjectComparer<TResult>(expected, actual, processor, enforceReferences, ignoreList,
                        ignoreDateTimeMilliseconds).CompareValues();
            }
        }

        public class AssertingObjectComparer : ObjectComparer
        {
            public static readonly AssertingProcessor AssertResult = new AssertingProcessor();
            public new static readonly PrintingProcessor StringResult = new PrintingProcessor();
            public new static readonly BoolProcessor BoolResult = new BoolProcessor();
        }

        

    }
}
