using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace PactNet.Models
{
    public class RegexMatcher : Matcher
    {
        [JsonProperty(PropertyName = "$type")] //TODO: Revisit this
        public string Type
        {
            get { return "PactNet, PactNet.Models.RegexMatcher"; }
        }

        public string Value { get; set; }

        public RegexMatcher(string value, string regex)
        {
            Value = value;
            Regex = regex;
        }
    }

    public class Matcher
    {
        [JsonProperty(PropertyName = "regex")]
        public string Regex { get; set; }

        public void Match(string item)
        {
            if (!String.IsNullOrEmpty(Regex))
            {
                var regex = new Regex(Regex);
                if (!regex.IsMatch(item))
                {
                    throw new CompareFailedException(Regex, item);
                }
            }
        }
    }

    public class Matchers
    {
        public static RegexMatcher Eg(string value, string regex)
        {
            return new RegexMatcher(value, regex);
        }
    }
}
