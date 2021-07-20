using System;
using IdApp.Pages.Main.Main;
using IdApp.Services;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Tests.Extensions;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Tests.ViewModels
{
    public class MainViewModelTests : ViewModelTests<MainViewModel>
    {
        private readonly Mock<ILogService> logService;
        private readonly Mock<IAttachmentCacheService> attachmentCacheService;
        private readonly Mock<INeuronService> neuronService;
        private readonly Mock<INeuronContracts> neuronContracts;
        private readonly Mock<IUiDispatcher> uiDispatcher;
        private readonly Mock<ITagProfile> tagProfile;
        private readonly Mock<INetworkService> networkService;
        private readonly Mock<INavigationService> navigationService;
        private readonly Mock<IContractOrchestratorService> contractOrchestratorService;
        private readonly Mock<IThingRegistryOrchestratorService> thingRegistryOrchestratorService;
        private readonly Mock<IEDalerOrchestratorService> eDalerOrchestratorService;

        public MainViewModelTests()
        {
            this.logService = new Mock<ILogService>();
            this.attachmentCacheService = new Mock<IAttachmentCacheService>();
            this.neuronService = new Mock<INeuronService>();
            this.neuronContracts = new Mock<INeuronContracts>();
            this.uiDispatcher = new Mock<IUiDispatcher>();
            this.navigationService = new Mock<INavigationService>();
            this.contractOrchestratorService = new Mock<IContractOrchestratorService>();
            this.thingRegistryOrchestratorService = new Mock<IThingRegistryOrchestratorService>();
            this.eDalerOrchestratorService = new Mock<IEDalerOrchestratorService>();
            this.tagProfile = new Mock<ITagProfile>();
            this.tagProfile.SetupGet(x => x.Domain).Returns("domain");
            LegalIdentity legalIdentity = new LegalIdentity { Id = Guid.NewGuid().ToString(), State = IdentityState.Approved };
            Property[] properties =
            {
                new Property(Constants.XmppProperties.FirstName, "John"),
                new Property(Constants.XmppProperties.LastName, "Doe"),
                new Property(Constants.XmppProperties.City, "Town"),
                new Property(Constants.XmppProperties.Country, "SE")
            };
            legalIdentity.Properties = properties;
            this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(legalIdentity);
            this.networkService = new Mock<INetworkService>();
            this.neuronService.Setup(x => x.Contracts).Returns(this.neuronContracts.Object);
        }

        protected override MainViewModel AViewModel()
        {
            return new MainViewModel(this.logService.Object, this.neuronService.Object, this.uiDispatcher.Object, this.tagProfile.Object, 
                this.navigationService.Object, this.networkService.Object, this.attachmentCacheService.Object, 
                this.contractOrchestratorService.Object, this.thingRegistryOrchestratorService.Object,
                this.eDalerOrchestratorService.Object);
        }

        [Test]
        public void AssignsProperties_FromIdentity()
        {
            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => vm.FullName == "John Doe" && vm.City == "Town" && vm.Country == "SWEDEN")
                .Finally(async vm => await vm.Unbind());
        }

        [Test]
        public void SetsPropertiesToEmptyString_WhenLegalIdentityIsMissing()
        {
            this.tagProfile.SetupGet(x => x.LegalIdentity).Returns((LegalIdentity)null);
            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => vm.FullName == string.Empty && vm.City == string.Empty && vm.Country == string.Empty)
                .Finally(async vm => await vm.Unbind());
        }
    }
}