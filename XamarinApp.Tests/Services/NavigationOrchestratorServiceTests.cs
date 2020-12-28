using System;
using Moq;
using NUnit.Framework;
using Tag.Sdk.Core.Models;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.Tests.Services
{
    public class NavigationOrchestratorServiceTests
    {
        private readonly TestTagProfile tagProfile;
        private readonly Mock<INeuronService> neuronService;
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
            public TestNavigationOrchestratorService(TagProfile tagProfile, INeuronService neuronService, INetworkService networkService, INavigationService navigationService)
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
            this.networkService = new Mock<INetworkService>();
            this.navigationService = new Mock<INavigationService>();
            this.sut = new TestNavigationOrchestratorService(this.tagProfile, this.neuronService.Object, this.networkService.Object, this.navigationService.Object);
        }

        [SetUp]
        public void Setup()
        {
            this.sut.Load();
        }

        [TearDown]
        public void TearDown()
        {
            this.sut.Unload();
        }

        [Test]
        public void DownloadsLegalIdentity_WhenIdentityIsSet_AndStateIsCompleteOrWaitingForValidation()
        {
            Guid guid = Guid.NewGuid();
            this.tagProfile.SetLegalIdentity(new LegalIdentity { Id = guid.ToString() });
            this.tagProfile.IsCompleteOrWaitingForValidationValue = true;
            this.neuronService.Raise(x => x.Loaded += null, new LoadedEventArgs(true));
            Assert.AreEqual(1, this.sut.DownloadCount);
        }

        [Test]
        public void DoesNotDownloadLegalIdentity_WhenIdentityIsMissing_AndStateIsCompleteOrWaitingForValidation()
        {
            Guid guid = Guid.NewGuid();
            this.tagProfile.IsCompleteOrWaitingForValidationValue = true;
            this.neuronService.Raise(x => x.Loaded += null, new LoadedEventArgs(true));
            Assert.AreEqual(0, this.sut.DownloadCount);
        }

        [Test]
        public void DoesNotDownloadLegalIdentity_WhenIdentityIsSet_AndStateIsIncorrect()
        {
            Guid guid = Guid.NewGuid();
            this.tagProfile.SetLegalIdentity(new LegalIdentity { Id = guid.ToString() });
            this.tagProfile.IsCompleteOrWaitingForValidationValue = false;
            this.neuronService.Raise(x => x.Loaded += null, new LoadedEventArgs(true));
            Assert.AreEqual(0, this.sut.DownloadCount);
        }
    }
}