using IdApp.Pages.Registration.ValidateIdentity;
using IdApp.Services;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Tests;
using Tag.Neuron.Xamarin.UI.Tests.Extensions;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Tests.ViewModels.Registration
{
    public class ValidateIdentityViewModelTests : ViewModelTests<ValidateIdentityViewModel>
    {
        private readonly Mock<ITagProfile> tagProfile = new Mock<ITagProfile>();
        private readonly Mock<IUiDispatcher> dispatcher = new Mock<IUiDispatcher>();
        private readonly Mock<INeuronService> neuronService = new Mock<INeuronService>();
        private readonly Mock<INavigationService> navigationService = new Mock<INavigationService>();
        private readonly Mock<INeuronContracts> contracts = new Mock<INeuronContracts>();
        private readonly Mock<ISettingsService> settingsService = new Mock<ISettingsService>();
        private readonly Mock<INetworkService> networkService = new Mock<INetworkService>();
        private readonly Mock<ILogService> logService = new Mock<ILogService>();
        private readonly Mock<IAttachmentCacheService> attachmentCacheService = new Mock<IAttachmentCacheService>();

        public ValidateIdentityViewModelTests()
        {
            this.neuronService.Setup(x => x.Contracts).Returns(contracts.Object);
            MockForms.Init();
        }

        protected override ValidateIdentityViewModel AViewModel()
        {
            return new ValidateIdentityViewModel(tagProfile.Object, dispatcher.Object, neuronService.Object, navigationService.Object, this.settingsService.Object, this.networkService.Object, this.logService.Object, this.attachmentCacheService.Object);
        }

        [Test]
        [TestCase(IdentityState.Obsoleted, false)]
        [TestCase(IdentityState.Rejected, false)]
        [TestCase(IdentityState.Compromised, false)]
        [TestCase(IdentityState.Created, false)]
        [TestCase(IdentityState.Approved, true)]
        public void CantContinue_WhenIdentityState_IsOtherThanApproved(IdentityState state, bool shouldContinue)
        {
            this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(new LegalIdentity { State = state });

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => shouldContinue ? vm.ContinueCommand.IsEnabled() : vm.ContinueCommand.IsDisabled())
                .Finally(async vm => await vm.Unbind());
        }

        [Test]
        [TestCase(IdentityState.Obsoleted, false, false)]
        [TestCase(IdentityState.Obsoleted, true, false)]
        [TestCase(IdentityState.Rejected, false, false)]
        [TestCase(IdentityState.Rejected, true, false)]
        [TestCase(IdentityState.Compromised, false, false)]
        [TestCase(IdentityState.Compromised, true, false)]
        [TestCase(IdentityState.Created, false, false)]
        [TestCase(IdentityState.Created, true, true)]
        [TestCase(IdentityState.Approved, false, false)]
        [TestCase(IdentityState.Approved, true, false)]
        public void CantInvite_WhenIdentityState_IsOtherThanCreated_Or_NeuronServiceIsOffline(IdentityState state, bool isOnline, bool shouldContinue)
        {
            this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(new LegalIdentity { State = state });
            this.neuronService.SetupGet(x => x.IsOnline).Returns(isOnline);

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => shouldContinue ? vm.InviteReviewerCommand.IsEnabled() : vm.InviteReviewerCommand.IsDisabled())
                .Finally(async vm => await vm.Unbind());
        }
    }
}