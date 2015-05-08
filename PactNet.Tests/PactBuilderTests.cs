using System;
using NSubstitute;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using PactNet.Models;
using Xunit;

namespace PactNet.Tests
{
    public class PactBuilderTests
    {
        private IPactBuilder GetSubject()
        {
            return new PactBuilder();
        }

        private IPactBuilder GetSubject(Func<int, bool, string, IMockProviderService> mockProviderServiceFactory)
        {
            return new PactBuilder(mockProviderServiceFactory);
        }

        [Fact]
        public void ServiceConsumer_WithConsumerName_SetsConsumerName()
        {
            const string consumerName = "My Service Consumer";
            var pactBuilder = GetSubject();

            pactBuilder.ServiceConsumer(consumerName);

            Assert.Equal(consumerName, ((PactBuilder)pactBuilder).ConsumerName);
        }

        [Fact]
        public void ServiceConsumer_WithNullConsumerName_ThrowsArgumentException()
        {
            var pactBuilder = GetSubject();

            Assert.Throws<ArgumentException>(() => pactBuilder.ServiceConsumer(null));
        }

        [Fact]
        public void ServiceConsumer_WithEmptyConsumerName_ThrowsArgumentException()
        {
            var pactBuilder = GetSubject();

            Assert.Throws<ArgumentException>(() => pactBuilder.ServiceConsumer(String.Empty));
        }

        [Fact]
        public void HasPactWith_WithProviderName_SetsProviderName()
        {
            const string providerName = "My Service Provider";
            var pact = GetSubject();

            pact.HasPactWith(providerName);

            Assert.Equal(providerName, ((PactBuilder)pact).ProviderName);
        }

        [Fact]
        public void HasPactWith_WithNullProviderName_ThrowsArgumentException()
        {
            var pactBuilder = GetSubject();

            Assert.Throws<ArgumentException>(() => pactBuilder.HasPactWith(null));
        }

        [Fact]
        public void HasPactWith_WithEmptyProviderName_ThrowsArgumentException()
        {
            var pactBuilder = GetSubject();

            Assert.Throws<ArgumentException>(() => pactBuilder.HasPactWith(String.Empty));
        }

        [Fact]
        public void MockService_WhenCalled_StartIsCalledAndMockProviderServiceIsReturned()
        {
            var mockMockProviderService = Substitute.For<IMockProviderService>();

            var pactBuilder = GetSubject((port, enableSsl, providerName) => mockMockProviderService);

            var mockProviderService = pactBuilder.MockService(1234);

            mockMockProviderService.Received(1).Start();
            Assert.Equal(mockMockProviderService, mockProviderService);
        }

        [Fact]
        public void MockService_WhenCalledTwice_StopIsCalledTheSecondTime()
        {
            var mockMockProviderService = Substitute.For<IMockProviderService>();

            var pactBuilder = GetSubject((port, enableSsl, providerName) => mockMockProviderService);

            pactBuilder.MockService(1234);
            mockMockProviderService.Received(0).Stop();

            pactBuilder.MockService(1234);
            mockMockProviderService.Received(1).Stop();
        }

        [Fact]
        public void MockService_WhenCalled_MockProviderServiceFactoryIsInvokedWithSslNotEnabled()
        {
            var calledWithSslEnabled = false;
            var mockMockProviderService = Substitute.For<IMockProviderService>();

            var pactBuilder = GetSubject((port, enableSsl, providerName) =>
            {
                calledWithSslEnabled = enableSsl;
                return mockMockProviderService;
            });

            pactBuilder.MockService(1234);

            Assert.False(calledWithSslEnabled);
        }

        [Fact]
        public void MockService_WhenCalledWithEnableSslFalse_MockProviderServiceFactoryIsInvokedWithSslNotEnabled()
        {
            var calledWithSslEnabled = false;
            var mockMockProviderService = Substitute.For<IMockProviderService>();

            var pactBuilder = GetSubject((port, enableSsl, providerName) =>
            {
                calledWithSslEnabled = enableSsl;
                return mockMockProviderService;
            });

            pactBuilder.MockService(1234, false);

            Assert.False(calledWithSslEnabled);
        }

        [Fact]
        public void MockService_WhenCalledWithEnableSslTrue_MockProviderServiceFactoryIsInvokedWithSslEnabled()
        {
            var calledWithSslEnabled = false;
            var mockMockProviderService = Substitute.For<IMockProviderService>();

            var pactBuilder = GetSubject((port, enableSsl, providerName) =>
            {
                calledWithSslEnabled = enableSsl;
                return mockMockProviderService;
            });

            pactBuilder.MockService(1234, true);

            Assert.True(calledWithSslEnabled);
        }

        [Fact]
        public void Build_WhenCalledBeforeTheMockProviderServiceIsInitialised_ThrowsInvalidOperationException()
        {
            var pactBuilder = GetSubject(null);

            Assert.Throws<InvalidOperationException>(() => pactBuilder.Build());
        }

        [Fact]
        public void Build_WhenCalledWithoutConsumerNameSet_ThrowsInvalidOperationException()
        {
            var pactBuilder = GetSubject((port, ssl, providerName) => Substitute.For<IMockProviderService>());
            pactBuilder.MockService(1234);
            pactBuilder
                .HasPactWith("Event API");

            Assert.Throws<InvalidOperationException>(() => pactBuilder.Build());
        }

        [Fact]
        public void Build_WhenCalledWithoutProviderNameSet_ThrowsInvalidOperationException()
        {
            var pactBuilder = GetSubject((port, ssl, providerName) => Substitute.For<IMockProviderService>());
            pactBuilder.MockService(1234);
            pactBuilder
                .ServiceConsumer("Event Client");

            Assert.Throws<InvalidOperationException>(() => pactBuilder.Build());
        }

        [Fact]
        public void Build_WhenCalledWithTheMockProviderServiceIsInitialised_CallsSendAdminHttpRequestOnTheMockProviderService()
        {
            const string consumerName = "Event Client";
            const string providerName = "Event API";
            var mockProviderService = Substitute.For<IMockProviderService>();

            var pactBuilder = GetSubject((port, ssl, provider) => mockProviderService);
            pactBuilder.MockService(1234);
            pactBuilder
                .ServiceConsumer(consumerName)
                .HasPactWith(providerName);

            pactBuilder.Build();

            mockProviderService.Received(1).SendAdminHttpRequest<PactDetails, PactFile>(HttpVerb.Post, Constants.PactPath, Arg.Is<PactDetails>(x => x.Consumer.Name == consumerName && x.Provider.Name == providerName));
        }

        [Fact]
        public void Build_WhenCalledWithAnInitialisedMockProviderService_StopIsCallOnTheMockServiceProvider()
        {
            var mockMockProviderService = Substitute.For<IMockProviderService>();

            var pactBuilder = GetSubject((port, enableSsl, providerName) => mockMockProviderService)
                .ServiceConsumer("Event Client")
                .HasPactWith("Event API");

            pactBuilder.MockService(1234);

            pactBuilder.Build();

            mockMockProviderService.Received(1).Stop();
        }

        [Fact]
        public void Build_WhenCalledWithTheMockProviderServiceIsInitialised_CallsSendAdminHttpRequestOnTheMockProviderServiceAndReturnsThePactFile()
        {
            const string consumerName = "Event Client";
            const string providerName = "Event API";
            var mockProviderService = Substitute.For<IMockProviderService>();

            var pactBuilder = GetSubject((port, ssl, provider) => mockProviderService);
            pactBuilder.MockService(1234);
            pactBuilder
                .ServiceConsumer(consumerName)
                .HasPactWith(providerName);

            var pactFile = new ProviderServicePactFile();
            mockProviderService.SendAdminHttpRequest<PactDetails, ProviderServicePactFile>(HttpVerb.Post, Constants.PactPath, Arg.Any<PactDetails>())
                .Returns(pactFile);

            var pact = pactBuilder.Build<ProviderServicePactFile>();

            Assert.Equal(pactFile, pact);
            mockProviderService.Received(1).SendAdminHttpRequest<PactDetails, ProviderServicePactFile>(HttpVerb.Post, Constants.PactPath, Arg.Is<PactDetails>(x => x.Consumer.Name == consumerName && x.Provider.Name == providerName));
        }
    }
}
