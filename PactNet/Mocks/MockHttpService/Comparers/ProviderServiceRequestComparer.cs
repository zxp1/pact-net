using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using PactNet.Comparers;
using PactNet.Mocks.MockHttpService.Models;

namespace PactNet.Mocks.MockHttpService.Comparers
{
    internal class ProviderServiceRequestComparer : IProviderServiceRequestComparer
    {
        private readonly IHttpMethodComparer _httpMethodComparer;
        private readonly IHttpPathComparer _httpPathComparer;
        private readonly IHttpQueryStringComparer _httpQueryStringComparer;
        private readonly IHttpHeaderComparer _httpHeaderComparer;
        private readonly IHttpBodyComparer _httpBodyComparer;

        public ProviderServiceRequestComparer()
        {
            _httpMethodComparer = new HttpMethodComparer();
            _httpPathComparer = new HttpPathComparer();
            _httpQueryStringComparer = new HttpQueryStringComparer();
            _httpHeaderComparer = new HttpHeaderComparer();
            _httpBodyComparer = new HttpBodyComparer();
        }

        public bool matchesItemInIgnoreList(JToken fieldToCheck, string[] ignoreList)
        {
            foreach (var item in ignoreList)
            {
                if (fieldToCheck.ToString().Contains(item))
                    return true;
            }
            return false;
        }

        public ComparisonResult Compare(ProviderServiceRequest expected, ProviderServiceRequest actual)
        {
            var result = new ComparisonResult("sends a request which");

            if (expected == null)
            {
                result.RecordFailure(new ErrorMessageComparisonFailure("Expected request cannot be null"));
                return result;
            }

            var methodResult = _httpMethodComparer.Compare(expected.Method, actual.Method);
            result.AddChildResult(methodResult);

            var pathResult = _httpPathComparer.Compare(expected.Path, actual.Path);
            result.AddChildResult(pathResult);

            var queryResult = _httpQueryStringComparer.Compare(expected.Query, actual.Query);
            result.AddChildResult(queryResult);

            if (expected.Headers != null && expected.Headers.Any())
            {
                var headerResult = _httpHeaderComparer.Compare(expected.Headers, actual.Headers);
                result.AddChildResult(headerResult);
            }

            //Remove items for the ignore from the list
            JToken expectedToken = JToken.FromObject(expected.Body).DeepClone();
            JToken actualToken = JToken.FromObject(actual.Body).DeepClone();

            //Expected
            List<JToken> removeListExpected = new List<JToken>();
            foreach (var bodyToken in expectedToken)
            {
                if (matchesItemInIgnoreList(bodyToken, expected.IgnoreList))
                {
                    removeListExpected.Add(bodyToken);
                }
            }

            foreach (JToken item in removeListExpected)
            {
                item.Remove();
            }

            //Actual
            List<JToken> removeListActual = new List<JToken>();
            foreach (var bodyToken in actualToken)
            {
                if (matchesItemInIgnoreList(bodyToken, expected.IgnoreList))
                {
                    removeListActual.Add(bodyToken);
                }
            }

            foreach (JToken item in removeListActual)
            {
                item.Remove();
            }

            var expectedToken2 = JToken.FromObject(expected.Body);
            var actualToken2 = JToken.FromObject(actual.Body);
            bool actualRequestMatchesExpectedRequest = false;
            try
            {
                actualRequestMatchesExpectedRequest = TestUtils.CheckAllPropertiesAreEqual(expectedToken2, actualToken2, expected.IgnoreList);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            if (actualRequestMatchesExpectedRequest)
            {
                result = new ComparisonResult("has a matching body");
            }
            else
            {
                result.RecordFailure(new ErrorMessageComparisonFailure("Expected request does not match actual request"));
            }
            
            return result;
        }
    }
}