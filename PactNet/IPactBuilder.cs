using PactNet.Mocks.MockHttpService;
using PactNet.Models;

namespace PactNet
{
    public interface IPactBuilder
    {
        IPactBuilder ServiceConsumer(string consumerName);
        IPactBuilder HasPactWith(string providerName);
        IMockProviderService MockService(int port, bool enableSsl = false);
        void Build();
        T Build<T>() where T : PactFile;
    }
}