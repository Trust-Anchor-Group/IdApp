using System;
using System.Threading.Tasks;
using IdApp.ViewModels;
using IdApp.Views.Registration;
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

        [Test]
        public void WhenIdentityIsRevoked_ThenRegistrationPageIsShown()
        {
            this.networkService
                .Setup(x => x.TryRequest(It.IsAny<Func<Task<LegalIdentity>>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.FromResult((true, new LegalIdentity())));

            this.uiDispatcher
                .Setup(x => x.DisplayAlert(AppResources.Confirm,
                    AppResources.AreYouSureYouWantToRevokeYourLegalIdentity, AppResources.Yes, AppResources.No))
                .Returns(Task.FromResult(true));

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .And(vm => ActionCommandIsExecuted(vm.RevokeCommand))
                .ThenAssert(() => this.navigationService.Verify(x => x.GoToAsync($"/{nameof(RegistrationPage)}"), Times.Once))
                .ThenAssert(() => this.uiDispatcher.Verify(x => x.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToRevokeYourLegalIdentity, AppResources.Yes, AppResources.No), Times.Once));
        }

        [Test]
        public void WhenIdentityIsCompromised_ThenRegistrationPageIsShown()
        {
            this.networkService
                .Setup(x => x.TryRequest(It.IsAny<Func<Task<LegalIdentity>>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.FromResult((true, new LegalIdentity())));

            this.uiDispatcher
                .Setup(x => x.DisplayAlert(AppResources.Confirm,
                    AppResources.AreYouSureYouWantToReportYourLegalIdentityAsCompromized, AppResources.Yes, AppResources.No))
                .Returns(Task.FromResult(true));

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .And(vm => ActionCommandIsExecuted(vm.CompromiseCommand))
                .ThenAssert(() => this.navigationService.Verify(x => x.GoToAsync($"/{nameof(RegistrationPage)}"), Times.Once))
                .ThenAssert(() => this.uiDispatcher.Verify(x => x.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToReportYourLegalIdentityAsCompromized, AppResources.Yes, AppResources.No), Times.Once));
        }
    }
}