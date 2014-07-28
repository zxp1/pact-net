using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PactNet.Models;

namespace PactNet.Mocks.MockHttpService.Comparers
{
    public class HttpBodyComparer : IHttpBodyComparer
    {
        private readonly string _messagePrefix;
        private int _comparisonPasses;

        public HttpBodyComparer(string messagePrefix)
        {
            _messagePrefix = messagePrefix;
        }

        //TODO: Remove boolean and add "matching" functionality
        public void Validate(dynamic body1, dynamic body2, IDictionary<string, Matcher> matchers = null, bool useStrict = false)
        {
            _comparisonPasses = 0;

            if (body1 == null)
            {
                return;
            }

            if (body1 != null && body2 == null)
            {
                throw new CompareFailedException("Body is null");
            }

            string body1Json = JsonConvert.SerializeObject(body1);
            string body2Json = JsonConvert.SerializeObject(body2);
            var httpBody1 = JsonConvert.DeserializeObject<JToken>(body1Json);
            var httpBody2 = JsonConvert.DeserializeObject<JToken>(body2Json);

            if (useStrict)
            {
                if (!JToken.DeepEquals(httpBody1, httpBody2))
                {
                    throw new CompareFailedException(httpBody1, httpBody2);
                }
                return;
            }

            if (httpBody1.Type == JTokenType.Array)
            {
                foreach (var element1 in httpBody1)
                {
                    foreach (var element2 in httpBody2)
                    {
                        try
                        {
                            AssertPropertyValuesMatch(element1, element2, matchers);
                            element2.Remove();
                            break;
                        }
                        catch (CompareFailedException)
                        {
                            if (element2 == httpBody2.Last)
                            {
                                throw new CompareFailedException(httpBody1, httpBody2);
                            }
                        }
                    }
                }
            }
            else
            {
                AssertPropertyValuesMatch(httpBody1, httpBody2, matchers);
            }
        }


        private void AssertPropertyValuesMatch(JToken httpBody1, JToken httpBody2, IDictionary<string, Matcher> matchers = null)
        {
            _comparisonPasses++;
            if (_comparisonPasses > 200)
            {
                throw new CompareFailedException("Too many passes required to compare objects.");
            }

            if (httpBody1 == null)
            {
                return;
            }
            if (httpBody1 != null && httpBody2 == null)
            {
                throw new CompareFailedException(httpBody1, "");
            }

            switch (httpBody1.Type)
            {
                case JTokenType.Array:
                    if (httpBody1.Count() != httpBody2.Count())
                    {
                        throw new CompareFailedException(httpBody1, httpBody2);
                    }

                    for (var i = 0; i < httpBody1.Count(); i++)
                    {
                        AssertPropertyValuesMatch(httpBody1[i], httpBody2[i], matchers);
                    }
                    break;
                
                case JTokenType.Object:
                    foreach (JProperty item1 in httpBody1)
                    {
                        var item2 = httpBody2.Cast<JProperty>().SingleOrDefault(x => x.Name == item1.Name);
                        AssertPropertyValuesMatch(item1, item2, matchers);
                    }
                    break;
            
                case JTokenType.Property:
                    AssertPropertyValuesMatch(httpBody1.SingleOrDefault(), httpBody2.SingleOrDefault(), matchers);
                    break;

                case JTokenType.Integer:
                case JTokenType.String:
                    var path = ToJsonPath(httpBody1.Path);
                    if (matchers != null && matchers.ContainsKey(path))
                    {
                        matchers[path].Match(httpBody2.ToString());
                    } 
                    else if (!httpBody1.Equals(httpBody2))
                    {
                        throw new CompareFailedException(httpBody1, httpBody2);
                    }
                    break;
                
                default:
                    if (!JToken.DeepEquals(httpBody1, httpBody2))
                    {
                        throw new CompareFailedException(httpBody1, httpBody2);
                    }
                    break;
            }
        }

        private string ToJsonPath(string jsonNetPath)
        {
            string key;
            var index = jsonNetPath.IndexOf('.');
            if (index > -1)
            {
                key = jsonNetPath.Substring(index, jsonNetPath.Length - index);
            }
            else
            {
                key = "." + jsonNetPath; //TODO: Maybe strip the . and add to the return
            }

            return "$" + key;
        }
    }
}