using System;
using IdApp.ViewModels;
using Moq;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Tests.ViewModels
{
    public class MainViewModelTests : ViewModelTests<MainViewModel>
    {
        private readonly Mock<INeuronService> neuronService;
        private readonly Mock<INeuronContracts> neuronContracts;
        private readonly Mock<IUiDispatcher> uiDispatcher;
        private readonly ITagProfile tagProfile;
        private readonly Mock<INetworkService> networkService;
        private readonly Mock<ILogService> logService;
        private readonly Mock<INavigationService> navigationService;

        public MainViewModelTests()
        {
            this.neuronService = new Mock<INeuronService>();
            this.neuronContracts = new Mock<INeuronContracts>();
            this.uiDispatcher = new Mock<IUiDispatcher>();
            this.tagProfile = new TagProfile();
            this.tagProfile.SetDomain("domain");
            this.tagProfile.SetLegalIdentity(new LegalIdentity { Id = Guid.NewGuid().ToString() });
            this.networkService = new Mock<INetworkService>();
            this.logService = new Mock<ILogService>();
            this.navigationService = new Mock<INavigationService>();
            this.neuronService.Setup(x => x.Contracts).Returns(this.neuronContracts.Object);
        }

        protected override MainViewModel AViewModel()
        {
            return new MainViewModel(this.neuronService.Object, this.uiDispatcher.Object, this.tagProfile, this.networkService.Object, this.logService.Object, this.navigationService.Object);
        }
    }
}