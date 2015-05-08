using System;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using PactNet.Models;

namespace PactNet
{
    public class PactBuilder : IPactBuilder
    {
        public string ConsumerName { get; private set; }
        public string ProviderName { get; private set; }
        private readonly Func<int, bool, string, IMockProviderService> _mockProviderServiceFactory;
        private IMockProviderService _mockProviderService;
        
        internal PactBuilder(
            Func<int, bool, string, IMockProviderService> mockProviderServiceFactory)
        {
            _mockProviderServiceFactory = mockProviderServiceFactory;
        }

        public PactBuilder()
            : this((port, enableSsl, providerName) => new MockProviderService(port, enableSsl, providerName))
        {
        }

        public IPactBuilder ServiceConsumer(string consumerName)
        {
            if (String.IsNullOrEmpty(consumerName))
            {
                throw new ArgumentException("Please supply a non null or empty consumerName");
            }

            ConsumerName = consumerName;

            return this;
        }

        public IPactBuilder HasPactWith(string providerName)
        {
            if (String.IsNullOrEmpty(providerName))
            {
                throw new ArgumentException("Please supply a non null or empty providerName");
            }

            ProviderName = providerName;

            return this;
        }

        public IMockProviderService MockService(int port, bool enableSsl = false)
        {
            if (_mockProviderService != null)
            {
                _mockProviderService.Stop();
            }

            _mockProviderService = _mockProviderServiceFactory(port, enableSsl, ProviderName);

            _mockProviderService.Start();

            return _mockProviderService;
        }

        public void Build()
        {
            Build<PactFile>();
        }

        public T Build<T>() where T : PactFile
        {
            if (_mockProviderService == null)
            {
                throw new InvalidOperationException("The Pact file could not be saved because the mock provider service is not initialised. Please initialise by calling the MockService() method.");
            }

            var pactFile = PersistPactFile<T>();
            _mockProviderService.Stop();

            return pactFile;
        }

        private T PersistPactFile<T>() where T : PactFile
        {
            if (String.IsNullOrEmpty(ConsumerName))
            {
                throw new InvalidOperationException("ConsumerName has not been set, please supply a consumer name using the ServiceConsumer method.");
            }

            if (String.IsNullOrEmpty(ProviderName))
            {
                throw new InvalidOperationException("ProviderName has not been set, please supply a provider name using the HasPactWith method.");
            }

            var pactDetails = new PactDetails
            {
                Provider = new Pacticipant { Name = ProviderName },
                Consumer = new Pacticipant { Name = ConsumerName }
            };

            var pactFile = _mockProviderService.SendAdminHttpRequest<PactDetails, T>(HttpVerb.Post, Constants.PactPath, pactDetails);

            return pactFile;
        }
    }
}
