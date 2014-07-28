using PactNet.Mocks.MockHttpService.Models;

namespace PactNet.Mocks.MockHttpService.Comparers
{
    public interface IPactProviderServiceResponseComparer //: IComparer<PactProviderServiceResponse>
    {
        void Compare(PactProviderServiceResponse response1, PactProviderServiceResponse response2,
            PactProviderResponseMatchingRules responseMatchingRules = null);
    }
}