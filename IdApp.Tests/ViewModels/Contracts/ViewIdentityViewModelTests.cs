﻿using IdApp.ViewModels.Contracts;
using IdApp.Views.Registration;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Tests;
using Tag.Neuron.Xamarin.UI.Tests.Extensions;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Tests.ViewModels.Contracts
{
    public class ViewIdentityViewModelTests : ViewModelTests<ViewIdentityViewModel>
    {
        private readonly ITagProfile tagProfile;
        private readonly Mock<IUiDispatcher> uiDispatcher;
        private readonly Mock<ILogService> logService;
        private readonly Mock<INeuronService> neuronService;
        private readonly Mock<INeuronContracts> neuronContracts;
        private readonly Mock<INavigationService> navigationService;
        private readonly Mock<INetworkService> networkService;
        private readonly LegalIdentity identity;
        private readonly SignaturePetitionEventArgs identityToReview;

        public ViewIdentityViewModelTests()
        {
            this.identity = new LegalIdentity { Id = Guid.NewGuid().ToString() };
            this.tagProfile = new TagProfile();
            this.tagProfile.SetDomain("domain");
            this.tagProfile.SetLegalIdentity(this.identity);
            this.uiDispatcher = new Mock<IUiDispatcher>();
            this.logService = new Mock<ILogService>();
            this.neuronService = new Mock<INeuronService>();
            this.neuronContracts = new Mock<INeuronContracts>();
            this.navigationService = new Mock<INavigationService>();
            this.networkService = new Mock<INetworkService>();
            this.identityToReview = null;
            this.neuronService.Setup(x => x.Contracts).Returns(this.neuronContracts.Object);
            MockForms.Init();
        }

        protected override ViewIdentityViewModel AViewModel()
        {
            return new ViewIdentityViewModel(this.tagProfile, this.uiDispatcher.Object, this.neuronService.Object, this.navigationService.Object, this.networkService.Object, this.logService.Object);
        }

        [Test]
        public void WhenIdentityIsRevoked_ThenRegistrationPageIsShown()
        {
            this.networkService
                .Setup(x => x.TryRequest(It.IsAny<Func<string, Task<LegalIdentity>>>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.FromResult((true, new LegalIdentity())));

            this.uiDispatcher
                .Setup(x => x.DisplayAlert(AppResources.Confirm,
                    AppResources.AreYouSureYouWantToRevokeYourLegalIdentity, AppResources.Yes, AppResources.No))
                .Returns(Task.FromResult(true));

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .And(vm => ActionCommandIsExecuted(vm.RevokeCommand))
                .ThenAssert(() => this.navigationService.Verify(x => x.GoToAsync($"///{nameof(RegistrationPage)}"), Times.Once))
                .ThenAssert(() => this.uiDispatcher.Verify(x => x.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToRevokeYourLegalIdentity, AppResources.Yes, AppResources.No), Times.Once));
        }

        [Test]
        public void WhenIdentityIsCompromised_ThenRegistrationPageIsShown()
        {
            this.networkService
                .Setup(x => x.TryRequest(It.IsAny<Func<string, Task<LegalIdentity>>>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.FromResult((true, new LegalIdentity())));

            this.uiDispatcher
                .Setup(x => x.DisplayAlert(AppResources.Confirm,
                    AppResources.AreYouSureYouWantToReportYourLegalIdentityAsCompromized, AppResources.Yes, AppResources.No))
                .Returns(Task.FromResult(true));

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .And(vm => ActionCommandIsExecuted(vm.CompromiseCommand))
                .ThenAssert(() => this.navigationService.Verify(x => x.GoToAsync($"///{nameof(RegistrationPage)}"), Times.Once))
                .ThenAssert(() => this.uiDispatcher.Verify(x => x.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToReportYourLegalIdentityAsCompromized, AppResources.Yes, AppResources.No), Times.Once));
        }
    }
}