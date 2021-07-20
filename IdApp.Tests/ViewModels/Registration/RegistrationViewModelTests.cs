using System;
using IdApp.Pages.Registration.RegisterIdentity;
using IdApp.Pages.Registration.Registration;
using IdApp.Services;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Extensions;
using Tag.Neuron.Xamarin.UI.Tests;
using Tag.Neuron.Xamarin.UI.Tests.Extensions;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Tests.ViewModels.Registration
{
    public class RegistrationViewModelTests : ViewModelTests<RegistrationViewModel>
    {
        private readonly Mock<ITagProfile> tagProfile = new Mock<ITagProfile>();
        private readonly Mock<IUiDispatcher> dispatcher = new Mock<IUiDispatcher>();
        private readonly Mock<INeuronService> neuronService = new Mock<INeuronService>();
        private readonly Mock<ICryptoService> cryptoService = new Mock<ICryptoService>();
        private readonly Mock<INavigationService> navigationService = new Mock<INavigationService>();
        private readonly Mock<INeuronContracts> contracts = new Mock<INeuronContracts>();
        private readonly Mock<ISettingsService> settingsService = new Mock<ISettingsService>();
        private readonly Mock<INetworkService> networkService = new Mock<INetworkService>();
        private readonly Mock<ILogService> logService = new Mock<ILogService>();
        private readonly Mock<IAttachmentCacheService> attachmentCacheService = new Mock<IAttachmentCacheService>();

        public RegistrationViewModelTests()
        {
            this.neuronService.Setup(x => x.Contracts).Returns(contracts.Object);
            MockForms.Init();
        }

        protected override RegistrationViewModel AViewModel()
        {
            return new RegistrationViewModel(tagProfile.Object, dispatcher.Object, this.settingsService.Object, neuronService.Object, cryptoService.Object, navigationService.Object, this.networkService.Object, this.logService.Object, this.attachmentCacheService.Object);
        }

        [Test]
        public void IdentityProperties_AreCopied_WhenMovingForward_AndChoosingAnExistingAccount()
        {
            LegalIdentity legalIdentity = new LegalIdentity { State = IdentityState.Approved };
            Property[] properties =
            {
                new Property(Constants.XmppProperties.FirstName, "John"),
                new Property(Constants.XmppProperties.LastName, "Doe"),
                new Property(Constants.XmppProperties.City, "Town")
            };
            legalIdentity.Properties = properties;
            this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(legalIdentity);

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm =>
                {
                    // Pre-condition
                    RegisterIdentityViewModel rvm = (RegisterIdentityViewModel)vm.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
                    return rvm.FirstName is null && rvm.LastNames is null && rvm.City is null;
                })
                .When(vm =>
                {
                    // Move forward two steps
                    object sender = vm.RegistrationSteps[(int)RegistrationStep.Operator];
                    // Operator->Account
                    this.tagProfile.SetupGet(x => x.Step).Returns(RegistrationStep.Account);
                    vm.RegistrationStep_Completed(sender, EventArgs.Empty);
                    // Account->RegisterIdentity
                    sender = vm.RegistrationSteps[(int)RegistrationStep.Account];
                    this.tagProfile.SetupGet(x => x.Step).Returns(RegistrationStep.RegisterIdentity);
                    vm.RegistrationStep_Completed(sender, EventArgs.Empty);
                })
                .ThenAssert(vm =>
                {
                    // Post-condition, verify properties have been copied from LegalIdentity to VM.
                    RegisterIdentityViewModel rvm = (RegisterIdentityViewModel)vm.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
                    return rvm.FirstName == "John" && rvm.LastNames == "Doe" && rvm.City == "Town";
                })
                .Finally(async vm => await vm.Unbind());
        }

        [Test]
        public void IdentityProperties_AreCopied_WhenMovingBackward_WithAnExistingAccount()
        {
            LegalIdentity legalIdentity = new LegalIdentity { State = IdentityState.Approved };
            Property[] properties =
            {
                new Property(Constants.XmppProperties.FirstName, "John"),
                new Property(Constants.XmppProperties.LastName, "Doe"),
                new Property(Constants.XmppProperties.City, "Town")
            };
            legalIdentity.Properties = properties;
            this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(legalIdentity);
            this.tagProfile.SetupGet(x => x.Step).Returns(RegistrationStep.ValidateIdentity);

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm =>
                {
                    // Pre-condition
                    RegisterIdentityViewModel rvm = (RegisterIdentityViewModel)vm.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
                    return rvm.FirstName is null && rvm.LastNames is null && rvm.City is null;
                })
                .When(vm =>
                {
                    vm.GoToPrevCommand.Execute();
                })
                .ThenAssert(vm =>
                {
                    // Post-condition, verify properties have been copied from LegalIdentity to VM.
                    RegisterIdentityViewModel rvm = (RegisterIdentityViewModel)vm.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
                    return rvm.FirstName == "John" && rvm.LastNames == "Doe" && rvm.City == "Town";
                })
                .Finally(async vm => await vm.Unbind());
        }
    }
}