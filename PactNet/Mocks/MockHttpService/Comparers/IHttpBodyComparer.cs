using System.Collections.Generic;
using PactNet.Models;

namespace PactNet.Mocks.MockHttpService.Comparers
{
    public interface IHttpBodyComparer
    {
        void Validate(dynamic body1, dynamic body2, IDictionary<string, Matcher> matchers = null, bool useStrict = false);
    }
}