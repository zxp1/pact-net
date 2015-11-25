using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using System.IO;

namespace PactNet.Comparers
{
    public interface IObjectMatcherProcessor<out TResult>
    {
        TResult MismatchObject(object expected, object actual, string hierarchy);
        TResult MismatchObjectReference(object expected, object actual, string hierarchy);
        TResult MismatchCollectionLength(int expected, int actual, ICollection expectedCollection, ICollection actualCollection, string hierarchy);

        TResult MisMatchEnumerableLength(int expected, int actual, IEnumerable expectedEnumerable,
                                         IEnumerable actualEnumerable, string hierarchy);

        TResult MismatchDictionaryKeys(ICollection expectedKeys, ICollection actualKeys, IDictionary expectedDictionary, IDictionary actualDictionary, string hierarchy);

        bool ShouldCompareProperty(object parentExpected, object parentActual, PropertyInfo property);
        bool ShouldCompareField(object parentExpected, object parentActual, FieldInfo field);

        TResult DefaultValue { get; }
    }

    public class ObjectComparer
    {
        public static readonly PrintingProcessor StringResult = new PrintingProcessor();
        public static readonly BoolProcessor BoolResult = new BoolProcessor();

        public static TResult CompareValues<TResult>(object expected, object actual, IObjectMatcherProcessor<TResult> processor, bool enforceReferences, IEnumerable<string> ignoreList = null, bool ignoreDateTimeMilliseconds = false)
        {
            return new ObjectComparer<TResult>(expected, actual, processor, enforceReferences, ignoreList, ignoreDateTimeMilliseconds).CompareValues();
        }
    }

    public class ObjectComparer<TResult>
    {
        private readonly object _expected;
        private readonly object _actual;
        private readonly IObjectMatcherProcessor<TResult> _processor;
        private readonly IList<object> _seenExpectedObjs;
        private readonly IList<object> _seenActualObjs;
        private readonly bool _enforceReferences;
        private readonly IEnumerable<string> _ignoreList;
        private readonly bool _ignoreDateTimeMilliseconds;

        /// <summary>
        /// Initialise an instance of the comparer object
        /// </summary>
        /// <param name="expected">Expected object (what to compare against)</param>
        /// <param name="actual">Actual Object (what we are comparing)</param>
        /// <param name="processor">Comparisson processor</param>
        /// <param name="enforceReferences">Should references equate (should they be the same object)</param>
        public ObjectComparer(
        object expected,
        object actual,
        IObjectMatcherProcessor<TResult> processor,
        bool enforceReferences)
            : this(expected,
                actual,
                processor,
                enforceReferences, new string[0],
            false)
        {
        }

        /// <summary>
        /// Initialise an instance of the comparer object
        /// </summary>
        /// <param name="expected">Expected object (what to compare against)</param>
        /// <param name="actual">Actual Object (what we are comparing)</param>
        /// <param name="processor">Comparisson processor</param>
        /// <param name="enforceReferences">Should references equate (should they be the same object)</param>
        /// <param name="ignoreList">String list of properties and fields to ignore during comparison</param>
        /// <param name="ignoreDateTimeMilliseconds">If set then the ticks of date times are not taken into account when comparing. This is useful when using serialised data.</param>
        public ObjectComparer(
            object expected,
            object actual,
            IObjectMatcherProcessor<TResult> processor,
            bool enforceReferences,
            IEnumerable<string> ignoreList, bool ignoreDateTimeMilliseconds)
        {
            _expected = expected;
            _ignoreDateTimeMilliseconds = ignoreDateTimeMilliseconds;
            _actual = actual;
            _processor = processor;
            _enforceReferences = enforceReferences;
            _ignoreList = ignoreList;
            _seenExpectedObjs = new List<object>();
            _seenActualObjs = new List<object>();
            _ignoreDateTimeMilliseconds = ignoreDateTimeMilliseconds;
        }


        public TResult CompareValues()
        {
            try
            {
                InternalCompareValues(_expected, _actual, String.Empty, _ignoreList);
            }
            catch (ResultsDifferException e)
            {
                return e.Result;
            }
            return _processor.DefaultValue;
        }

        private void InternalCompareValues(
            object expected,
            object actual,
            string hierarchy,
            IEnumerable<string> ignoreList//,IEnumerable<string> utcDateList
            )
        {
            if (ignoreList == null)
            {
                ignoreList = new string[0];
            }
            //if(utcDateList == null)
            //{
            //    utcDateList = new string[0];
            //}
            if (expected == null && actual == null)
                return;

            var isArraySubclass = false;
            if (expected != null && actual != null && expected.GetType().IsArray && actual.GetType().IsArray)
            {
                var expectedType = expected.GetType();
                var actualType = actual.GetType();
                var expectedElementType = expectedType.GetElementType();
                var actualElementType = actualType.GetElementType();
                isArraySubclass = expectedElementType.IsSubclassOf(actualElementType) ||
                                  actualElementType.IsSubclassOf(expectedElementType);
            }
            if (!isArraySubclass)
            {
                if ((expected == null && actual != null) || (expected != null && actual == null)
                    // make sure both are null or non-null
                    || (expected != null && actual != null && !expected.GetType().Equals(actual.GetType())))
                {
                    throw new ResultsDifferException(_processor.MismatchObject(expected, actual, hierarchy));
                }


                if (!actual.GetType().Equals(typeof(string)))
                {
                    // See TestAllPropertiesAreEqualSimple_ReferenceEqualityWhenCreatingNewCharBufferString
                    // Creating a new string buffer breaks reference equality unexpectedly
                    // XmlSerializer does this for some strange reason
                    for (int i = 0; i < _seenActualObjs.Count; i++)
                    {
                        if (ReferenceEquals(actual, _seenActualObjs[i]) ||
                            ReferenceEquals(expected, _seenExpectedObjs[i]))
                        {
                            if (ReferenceEquals(actual, _seenActualObjs[i]) &&
                                ReferenceEquals(expected, _seenExpectedObjs[i]))
                            {
                                // if both are equal, things are ok
                                return;
                            }
                            if (_enforceReferences)
                            {
                                // an expected identical reference was not found
                                throw new ResultsDifferException(_processor.MismatchObjectReference(expected, actual,
                                                                                                    hierarchy));
                            }
                        }
                    }
                }
            }

            _seenExpectedObjs.Add(expected);
            _seenActualObjs.Add(actual);

            if (expected is IDictionary)
            {
                CompareDictionaries((IDictionary)expected, (IDictionary)actual, hierarchy, ignoreList);
            }
            else if (expected is JObject)
            {
                CompareJObjects((JObject)expected, (JObject)actual, hierarchy, ignoreList);
            }
            else if (expected is ICollection)
            {
                CompareCollection((ICollection)expected, (ICollection)actual, hierarchy, ignoreList);
            }
            else if (expected is JToken)
            {
                var token = (JToken)expected;
                if (!ignoreList.Contains(token.Path))
                {
                    if (!expected.Equals(actual))
                    {
                        throw new ResultsDifferException(_processor.MismatchObject(expected, actual, hierarchy));
                    }
                }
            }
            else if (!(expected is string) && expected is IEnumerable)
            {
                // We treat strings as a non enumerable item for performance (and ease of error reporting)
                CompareIEnumerable((IEnumerable)expected, (IEnumerable)actual, hierarchy, ignoreList);
            }
            else if (expected is MemoryStream && actual is MemoryStream)
            {
                InternalCompareValues(((MemoryStream)expected).ToArray(), ((MemoryStream)actual).ToArray(), hierarchy, ignoreList);
            }
            else if ((PropertyUtils.IsCustomType(expected.GetType())
                        || expected is Exception)
                     && !expected.GetType().IsEnum)
            {
                IEnumerable<PropertyInfo> properties;
                //If type is derived from BaseDomainEntity then ignore the "CreatedAt" field comparision.
                if (Type.GetType("Leica.Bond.Common.DataAccess.BaseDomainEntity, Leica.Bond.Common.DataAccess") != null &&
                    expected.GetType().IsSubclassOf(Type.GetType("Leica.Bond.Common.DataAccess.BaseDomainEntity, Leica.Bond.Common.DataAccess")))
                {
                    properties = expected.GetType().GetProperties()
                        .Where(
                            p =>
                            p.CanRead && !ignoreList.Contains(p.Name) && p.Name != "CreatedAt" &&
                            _processor.ShouldCompareProperty(expected, actual, p));
                }
                else
                {
                    properties = expected.GetType().GetProperties()
                        .Where(
                            p =>
                            p.CanRead && !ignoreList.Contains(p.Name) &&
                            _processor.ShouldCompareProperty(expected, actual, p));
                }
                foreach (PropertyInfo property in properties)
                {
                    object obj1 = ReturnValueOrException(expected, o => property.GetValue(o, null));
                    object obj2 = ReturnValueOrException(actual, o => property.GetValue(o, null));
                    InternalCompareValues(obj1, obj2, JoinHierarchy(hierarchy, property.Name), ignoreList);
                }
                foreach (FieldInfo field in expected.GetType().GetFields()
                    .Where(f => f.IsPublic && !ignoreList.Contains(f.Name) && _processor.ShouldCompareField(expected, actual, f)))
                {
                    object obj1 = ReturnValueOrException(expected, o => field.GetValue(o));
                    object obj2 = ReturnValueOrException(actual, o => field.GetValue(o));
                    InternalCompareValues(obj1, obj2, JoinHierarchy(hierarchy, field.Name), ignoreList);
                }
            }
            else
            {
                TimeSpan tolerance = TimeSpan.FromSeconds(_ignoreDateTimeMilliseconds ? 1 : 0);

                if ((expected is DateTime?) &&
                    DoDateTimesMatchToWithinTolerance((DateTime?)expected, (DateTime?)actual, tolerance))
                    return;

                if ((expected is DateTime) &&
                    DoDateTimesMatchToWithinTolerance((DateTime)expected, (DateTime)actual, tolerance))
                    return;

                if (!expected.Equals(actual))
                {
                    throw new ResultsDifferException(_processor.MismatchObject(expected, actual, hierarchy));
                }
            }
        }

        private void CompareJObjects(JObject expected, JObject actual, string hierarchy, IEnumerable<string> ignoreList)
        {
            var expTokenNames = expected.Children().Select(n => JoinHierarchy(hierarchy, ((JProperty)n).Name)).ToArray();
            var actTokenNames = actual.Children().Select(n => JoinHierarchy(hierarchy, ((JProperty)n).Name)).ToArray();
            // More fields in expected (i.e. fields are present in expected which aren't present in actual or ignore) should result in a fail:
            var missingTokenNames = expTokenNames.Except(ignoreList).Except(actTokenNames).ToArray();
            if (missingTokenNames.Any())
                throw new ResultsDifferException(_processor.MismatchObject(string.Join(", ", missingTokenNames), null, hierarchy));
            // Add any extra tokens to ignore list, as in this case we don't care about them
            ignoreList = ignoreList.Concat(actTokenNames.Except(expTokenNames)).Distinct();

            var expectedEnum = expected.GetEnumerator();
            var actualEnum = actual.GetEnumerator();
            while (expectedEnum.MoveNext() && actualEnum.MoveNext())
            {
                if (!ignoreList.Contains(JoinHierarchy(hierarchy, expectedEnum.Current.Key)))
                {
                    var movedToNext = true;
                    // loop until an object not being ignored is encountered
                    while ((ignoreList.Contains(JoinHierarchy(hierarchy, actualEnum.Current.Key)) || actualEnum.Current.Key == "$type") &&
                           (movedToNext = actualEnum.MoveNext()))
                        ;

                    if (movedToNext)
                    {
                        if (expectedEnum.Current.Key != actualEnum.Current.Key)
                            throw new ResultsDifferException(_processor.MismatchObject(expectedEnum.Current.Key,
                                actualEnum.Current.Key, hierarchy));
                        InternalCompareValues(expectedEnum.Current.Value, actualEnum.Current.Value,
                            JoinHierarchy(hierarchy, actualEnum.Current.Key), ignoreList);
                        int a = 0;
                    }
                }
            }
        }

        // one Kind unset ... return false
        // both Kind set? UTC them
        // both Kind unset? UTC them
        public static bool DoDateTimesMatchToWithinTolerance(DateTime expected, DateTime actual, TimeSpan tolerance)
        {
            var sameKind = expected.Kind == actual.Kind;
            var hasUnspec = (expected.Kind == DateTimeKind.Unspecified || actual.Kind == DateTimeKind.Unspecified);
            if (!sameKind && hasUnspec) return false;

            return (actual.ToUniversalTime().Subtract(expected.ToUniversalTime()).Duration() < tolerance);
        }

        public static bool DoDateTimesMatchToWithinTolerance(DateTime? expected, DateTime? actual, TimeSpan tolerance)
        {
            if (expected.HasValue == actual.HasValue)
                return expected.HasValue
                    ? DoDateTimesMatchToWithinTolerance(expected.Value, actual.Value, tolerance) // Both 'have value'.
                    : true; // Both are null.

            return false; // Only one 'has value'.
        }

        private void CompareCollection(ICollection expectedCol, ICollection actualCol, string hierarchy, IEnumerable<string> ignoreList)
        {
            if (expectedCol.Count != actualCol.Count)
            {
                throw new ResultsDifferException(_processor.MismatchCollectionLength(expectedCol.Count,
                                                                                     actualCol.Count, expectedCol,
                                                                                     actualCol, hierarchy));
            }
            var expectedEnum = expectedCol.GetEnumerator();
            var actualEnum = actualCol.GetEnumerator();
            int index = 0;
            while (expectedEnum.MoveNext() && actualEnum.MoveNext())
            {
                InternalCompareValues(expectedEnum.Current, actualEnum.Current, hierarchy + "[" + index + "]", ignoreList);
                index++;
            }
        }

        private void CompareDictionaries(IDictionary expectedDict, IDictionary actualDict, string hierarchy, IEnumerable<string> ignoreList)
        {
            if (expectedDict.Keys.Count != actualDict.Keys.Count)
            {
                throw new ResultsDifferException(_processor.MismatchDictionaryKeys(expectedDict.Keys, actualDict.Keys,
                    expectedDict, actualDict, hierarchy));
            }

            foreach (var key in expectedDict.Keys)
            {
                if (actualDict.Contains(key))
                {
                    InternalCompareValues(expectedDict[key], actualDict[key], hierarchy + "[" + key + "]", ignoreList);
                }
                else
                {
                    throw new ResultsDifferException(_processor.MismatchObject(expectedDict[key], null, hierarchy));
                }
            }
        }

        private void CompareIEnumerable(IEnumerable expectedCol, IEnumerable actualCol, string hierarchy, IEnumerable<string> ignoreList)
        {
            var expectedEnum = expectedCol.GetEnumerator();
            var actualEnum = actualCol.GetEnumerator();
            var index = 0;
            var expectedHasElement = expectedEnum.MoveNext();
            var actualHasElement = actualEnum.MoveNext();

            while (expectedHasElement || actualHasElement)
            {
                if (expectedHasElement != actualHasElement)
                {
                    throw new ResultsDifferException(_processor.MisMatchEnumerableLength(
                        expectedHasElement ? index + 1 : index,
                        actualHasElement ? index + 1 : index,
                        expectedCol, actualCol, hierarchy
                    ));
                }
                InternalCompareValues(expectedEnum.Current, actualEnum.Current, hierarchy + "[" + index + "]", ignoreList);
                index++;

                expectedHasElement = expectedEnum.MoveNext();
                actualHasElement = actualEnum.MoveNext();
            }
        }

        public static string removeSquareBracketsForArray(string s)
        {
            bool checkForBrackets = false;
            do
            {
                int firstBracket = s.IndexOf("[");
                int secondBracket = s.IndexOf("]");
                if (firstBracket < secondBracket && firstBracket != -1)
                    //if firstBracket exists in the string and is before second bracket
                {
                    string stringBetweenBrackets = s.Substring(firstBracket + 1,
                        secondBracket - firstBracket - 1);
                    if (IsAllDigits(stringBetweenBrackets))
                    {
                        s = s.Remove(firstBracket, secondBracket - firstBracket + 1);
                        checkForBrackets = true;
                    }
                }
                else
                {
                    checkForBrackets = false; //No more brackets exist
                }
            } while (checkForBrackets); 
            return s;
        }

        static bool IsAllDigits(string s)
        {
            return s.All(Char.IsDigit);
        }

        private static string JoinHierarchy(string hierarchy, string addition)
        {
            hierarchy = removeSquareBracketsForArray(hierarchy);
            return (hierarchy + "." + addition).Trim('.');
        }

        private class ResultsDifferException : Exception
        {
            private readonly TResult _result;

            public ResultsDifferException(TResult result)
            {
                _result = result;
            }

            public TResult Result
            {
                get { return _result; }
            }
        }

        private static object ReturnValueOrException(object o, Func<object, object> fetchValue)
        {
            try
            {
                return fetchValue(o);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    return e.InnerException;
                return e;
            }
        }
    }
}

