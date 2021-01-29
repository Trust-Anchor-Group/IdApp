using IdApp.Services;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Models;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Tests.Services
{
    public class ContractOrchestratorServiceTests
    {
        private readonly TestTagProfile tagProfile;
        private readonly Mock<IUiDispatcher> uiDispatcher;
        private readonly Mock<INeuronService> neuronService;
        private readonly Mock<INeuronContracts> neuronContracts;
        private readonly Mock<INetworkService> networkService;
        private readonly Mock<INavigationService> navigationService;
        private readonly Mock<ILogService> logService;
        private readonly TestContractOrchestratorService sut;

        private class TestTagProfile : TagProfile
        {
            public TestTagProfile(params DomainModel[] domainModels)
            : base(domainModels)
            {
            }

            public bool IsCompleteOrWaitingForValidationValue { get; set; }

            public override bool IsCompleteOrWaitingForValidation()
            {
                return IsCompleteOrWaitingForValidationValue;
            }
        }

        private class TestContractOrchestratorService : ContractOrchestratorService
        {
            public TestContractOrchestratorService(ITagProfile tagProfile, IUiDispatcher uiDispatcher, INeuronService neuronService, INavigationService navigationService, ILogService logService, INetworkService networkService)
            : base(tagProfile, uiDispatcher, neuronService, navigationService, logService, networkService)
            {
            }

            public int DownloadCount { get; set; }

            protected override void DownloadLegalIdentityInternal(string legalId)
            {
                DownloadCount++;
            }
        }

        public ContractOrchestratorServiceTests()
        {
            this.tagProfile = new TestTagProfile();
            this.neuronService = new Mock<INeuronService>();
            this.neuronContracts = new Mock<INeuronContracts>();
            this.networkService = new Mock<INetworkService>();
            this.navigationService = new Mock<INavigationService>();
            this.neuronService.SetupGet(x => x.Contracts).Returns(this.neuronContracts.Object);
            this.logService = new Mock<ILogService>();
            this.uiDispatcher = new Mock<IUiDispatcher>();
            this.sut = new TestContractOrchestratorService(this.tagProfile, this.uiDispatcher.Object, this.neuronService.Object, this.navigationService.Object, this.logService.Object, this.networkService.Object);
        }

        [SetUp]
        public void Setup()
        {
            this.sut.Load(false);
        }

        [TearDown]
        public void TearDown()
        {
            this.sut.Unload();
        }

        [Test]
        public async Task DownloadsLegalIdentity_WhenIdentityIsSet_AndStateIsCompleteOrWaitingForValidation()
        {
            Guid guid = Guid.NewGuid();
            this.neuronService.SetupGet(x => x.IsOnline).Returns(true);
            this.tagProfile.SetLegalIdentity(new LegalIdentity { Id = guid.ToString() });
            this.tagProfile.IsCompleteOrWaitingForValidationValue = true;
            this.neuronContracts.Raise(x => x.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(XmppState.Connected));
            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.AreEqual(1, this.sut.DownloadCount);
        }

        [Test]
        public void DoesNotDownloadLegalIdentity_WhenIdentityIsMissing_AndStateIsCompleteOrWaitingForValidation()
        {
            Guid guid = Guid.NewGuid();
            this.neuronService.SetupGet(x => x.IsOnline).Returns(true);
            this.tagProfile.IsCompleteOrWaitingForValidationValue = true;
            this.neuronContracts.Raise(x => x.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(XmppState.Connected));
            Assert.AreEqual(0, this.sut.DownloadCount);
        }

        [Test]
        public void DoesNotDownloadLegalIdentity_WhenIdentityIsSet_AndStateIsIncorrect()
        {
            this.neuronService.SetupGet(x => x.IsOnline).Returns(true);
            Guid guid = Guid.NewGuid();
            this.tagProfile.SetLegalIdentity(new LegalIdentity { Id = guid.ToString() });
            this.tagProfile.IsCompleteOrWaitingForValidationValue = false;
            this.neuronContracts.Raise(x => x.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(XmppState.Connected));
            Assert.AreEqual(0, this.sut.DownloadCount);
        }
    }
}