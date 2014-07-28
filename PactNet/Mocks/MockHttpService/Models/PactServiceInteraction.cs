using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PactNet.Models;

namespace PactNet.Mocks.MockHttpService.Models
{
    public class PactServiceInteraction : PactInteraction
    {
        private PactProviderServiceResponse _response;

        [JsonProperty(PropertyName = "request")]
        public PactProviderServiceRequest Request { get; set; }

        [JsonProperty(PropertyName = "responseMatchingRules")]
        public PactProviderResponseMatchingRules ResponseMatchingRules { get; set; }

        [JsonProperty(PropertyName = "response")]
        public PactProviderServiceResponse Response
        {
            get { return _response; }
            set
            {
                //TODO: Another really nasty hack atm
                if (ResponseMatchingRules != null)
                {
                    _response = value;
                    return;
                }

                PactProviderResponseMatchingRules matchingRules = null;

                if (value.Body != null)
                {
                    //Iterate over all body properties, array items etc
                    //If item value is typeof(RegexMatcher)
                        //Store data on ResponseMatchingRules

                    if (value.Body is string || value.Body is int || value.Body is bool)
                    {
                    }
                    else if (value.Body is RegexMatcher)
                    {
                        var matcher = value.Body as RegexMatcher;

                        matchingRules = matchingRules ?? new PactProviderResponseMatchingRules();
                        matchingRules.Body = matchingRules.Body ?? new Dictionary<string, Matcher>();

                        matchingRules.Body.Add("$..*", matcher);

                        value.Body = matcher.Value;
                    }
                    else
                    {
                        //TODO: Use content-type headers to skip stuff
                        //TODO: This will only work for a specific object graph (Something recursive will probably work)

                        JToken body = JToken.FromObject(value.Body);
                        JToken body2 = body.DeepClone();
                        
                        foreach (var item in body) //Array
                        {
                            foreach (var item2 in item) //Body Item Object
                            {
                                foreach (var item3 in item2) //Properties
                                {
                                    foreach (JProperty item4 in item3)
                                    {
                                        if (item4.Name.Equals("$type") &&
                                            item4.Value.ToString().Equals("PactNet, PactNet.Models.RegexMatcher"))
                                        {
                                            var matcher = (RegexMatcher)JsonConvert.DeserializeObject(item3.ToString(),
                                                typeof(RegexMatcher));

                                            matchingRules = matchingRules ?? new PactProviderResponseMatchingRules();
                                            matchingRules.Body = matchingRules.Body ?? new Dictionary<string, Matcher>();

                                            var index = item3.Path.IndexOf('.');
                                            var key = item3.Path.Substring(index, item3.Path.Length - index);

                                            matchingRules.Body.Add("$" + key, new Matcher { Regex = matcher.Regex });

                                            body2.SelectToken(item3.Path).Replace(matcher.Value); //TODO: Maybe we dont need another copy of the object
                                        }
                                    }
                                }
                            }
                        }

                        if (matchingRules != null && matchingRules.Body != null && matchingRules.Body.Any())
                        {
                            value.Body = body2;
                        }
                    }
                }

                ResponseMatchingRules = matchingRules;

                _response = value;
            }
        }
    }

    public class PactProviderResponseMatchingRules
    {
        [JsonProperty(PropertyName = "body")]
        public IDictionary<string, Matcher> Body { get; set; }
    }
}