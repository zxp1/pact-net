using System.Collections.Generic;
using PactNet.Mocks.MockHttpService.Models;

namespace PactNet.Mocks.MockHttpService
{
    public interface IMockProviderService : IMockProvider<IMockProviderService>
    {
        IMockProviderService With(ProviderServiceRequest request);
        void WillRespondWith(ProviderServiceResponse response);
        void Start();
        void Stop();
        void ClearInteractions();
        void VerifyInteractions();
        TResponse SendAdminHttpRequest<TReqest, TResponse>(HttpVerb method, string path, TReqest requestContent, IDictionary<string, string> headers = null);
    }
}