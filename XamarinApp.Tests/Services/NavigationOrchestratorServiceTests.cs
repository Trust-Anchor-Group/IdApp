using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Tag.Sdk.Core.Models;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.Tests.Services
{
    public class NavigationOrchestratorServiceTests
    {
        private readonly TestTagProfile tagProfile;
        private readonly Mock<INeuronService> neuronService;
        private readonly Mock<INeuronContracts> neuronContracts;
        private readonly Mock<INetworkService> networkService;
        private readonly Mock<INavigationService> navigationService;
        private readonly TestNavigationOrchestratorService sut;

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

        private class TestNavigationOrchestratorService : NavigationOrchestratorService
        {
            public TestNavigationOrchestratorService(ITagProfile tagProfile, INeuronService neuronService, INetworkService networkService, INavigationService navigationService)
            : base(tagProfile, neuronService, networkService, navigationService)
            {
            }

            public int DownloadCount { get; set; }

            protected override void DownloadLegalIdentityInternal(string legalId)
            {
                DownloadCount++;
            }
        }

        public NavigationOrchestratorServiceTests()
        {
            this.tagProfile = new TestTagProfile();
            this.neuronService = new Mock<INeuronService>();
            this.neuronContracts = new Mock<INeuronContracts>();
            this.networkService = new Mock<INetworkService>();
            this.navigationService = new Mock<INavigationService>();
            this.neuronService.SetupGet(x => x.Contracts).Returns(this.neuronContracts.Object);
            this.sut = new TestNavigationOrchestratorService(this.tagProfile, this.neuronService.Object, this.networkService.Object, this.navigationService.Object);
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