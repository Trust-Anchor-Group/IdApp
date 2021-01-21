using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.Tests;
using Tag.Sdk.UI.Tests.Extensions;
using Tag.Sdk.UI.Tests.ViewModels;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.ViewModels.Contracts;
using XamarinApp.Views.Registration;

namespace XamarinApp.Tests.ViewModels.Contracts
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
                .Setup(x => x.Request(It.IsAny<Func<string, Task<LegalIdentity>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(Task.FromResult((true, new LegalIdentity())));

            this.uiDispatcher
                .Setup(x => x.DisplayAlert(AppResources.Confirm,
                    AppResources.AreYouSureYouWantToRevokeYourLegalIdentity, AppResources.Yes, AppResources.Cancel))
                .Returns(Task.FromResult(true));

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .And(vm => ActionCommandIsExecuted(vm.RevokeCommand))
                .ThenAssert(() => this.navigationService.Verify(x => x.ReplaceAsync(It.IsAny<RegistrationPage>()), Times.Once));
        }
    }
}