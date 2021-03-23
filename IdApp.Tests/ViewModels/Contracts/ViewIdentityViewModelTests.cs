using IdApp.ViewModels.Contracts;
using IdApp.Views.Registration;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Xml;
using IdApp.Navigation.Identity;
using IdApp.Services;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Tests;
using Tag.Neuron.Xamarin.UI.Tests.Extensions;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Tests.ViewModels.Contracts
{
    public class ViewIdentityViewModelTests : ViewModelTests<ViewIdentityViewModel>
    {
        private readonly Mock<ITagProfile> tagProfile;
        private readonly Mock<IUiDispatcher> uiDispatcher;
        private readonly Mock<ILogService> logService;
        private readonly Mock<INeuronService> neuronService;
        private readonly Mock<INeuronContracts> neuronContracts;
        private readonly Mock<INavigationService> navigationService;
        private readonly Mock<INetworkService> networkService;
        private readonly Mock<IImageCacheService> imageCacheService;

        public ViewIdentityViewModelTests()
        {
            this.tagProfile = new Mock<ITagProfile>();
            this.uiDispatcher = new Mock<IUiDispatcher>();
            this.logService = new Mock<ILogService>();
            this.neuronService = new Mock<INeuronService>();
            this.neuronContracts = new Mock<INeuronContracts>();
            this.navigationService = new Mock<INavigationService>();
            this.networkService = new Mock<INetworkService>();
            this.imageCacheService = new Mock<IImageCacheService>();
            this.neuronService.Setup(x => x.Contracts).Returns(this.neuronContracts.Object);
            MockForms.Init();
        }

        protected override ViewIdentityViewModel AViewModel()
        {
            return new ViewIdentityViewModel(this.tagProfile.Object, this.uiDispatcher.Object, this.neuronService.Object, this.navigationService.Object, this.networkService.Object, this.logService.Object, this.imageCacheService.Object);
        }

        [SetUp]
        public void Setup()
        {
            this.tagProfile.SetupGet(x => x.Domain).Returns("domain");
            this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(new LegalIdentity { Id = Guid.NewGuid().ToString() });
        }

        [TearDown]
        public void Teardown()
        {
            this.tagProfile.Reset();
            this.navigationService.Reset();
            this.uiDispatcher.Reset();
        }

        [Test]
        public void AssignsLegalIdentity_FromTagProfile_WhenArgsIsEmpty()
        {
            LegalIdentity legalIdentity = new LegalIdentity
            {
                Properties = new[]
                {
                    new Property(Constants.XmppProperties.Jid, "42")
                }
            };
            ViewIdentityNavigationArgs args = new ViewIdentityNavigationArgs(null, null);
            this.navigationService.Setup(x => x.TryPopArgs(out args)).Returns(true);
            this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(legalIdentity);

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => ReferenceEquals(legalIdentity, vm.LegalIdentity));
        }

        [Test]
        public void AssignsLegalIdentity_FromArgs_WhenSet()
        {
            LegalIdentity legalIdentityArgs = new LegalIdentity
            {
                Properties = new[]
                {
                    new Property(Constants.XmppProperties.Jid, "42")
                }
            };
            ViewIdentityNavigationArgs args = new ViewIdentityNavigationArgs(legalIdentityArgs, null);
            this.navigationService.Setup(x => x.TryPopArgs(out args)).Returns(true);
            this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(new LegalIdentity { Id = Guid.NewGuid().ToString() });

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => ReferenceEquals(legalIdentityArgs, vm.LegalIdentity));
        }

        [Test]
        public void AssignsBareJid_FromLegalIdentity_WhenIdentityToReviewIsNull()
        {
            LegalIdentity legalIdentity = new LegalIdentity
            {
                Properties = new[]
                {
                    new Property(Constants.XmppProperties.Jid, "42")
                }
            };
            ViewIdentityNavigationArgs args = new ViewIdentityNavigationArgs(legalIdentity, null);
            this.navigationService.Setup(x => x.TryPopArgs(out args)).Returns(true);

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => vm.BareJid == "42");
        }

        [Test]
        public void AssignsBareJid_FromIdentityToReview_WhenItExists()
        {
            LegalIdentity legalIdentity = new LegalIdentity
            {
                Properties = new[]
                {
                    new Property(Constants.XmppProperties.Jid, "42")
                }
            };
            LegalIdentity identityToReview = new LegalIdentity
            {
                Properties = new[]
                {
                    new Property(Constants.XmppProperties.Jid, "43")
                }
            };
            XmppClient client = new XmppClient("host", 4000, "user", "pass", string.Empty, GetType().Assembly);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<?xml version=\"1.0\" ?><root></root>");

            ViewIdentityNavigationArgs args = new ViewIdentityNavigationArgs(legalIdentity, 
                new SignaturePetitionEventArgs(new MessageEventArgs(client, doc.DocumentElement), identityToReview, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "purpose", new byte[0]));
            this.navigationService.Setup(x => x.TryPopArgs(out args)).Returns(true);

            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => vm.BareJid == "43");
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
                .ThenAssert(() => this.tagProfile.Verify(x => x.RevokeLegalIdentity(It.IsAny<LegalIdentity>()), Times.Once))
                .ThenAssert(() => this.navigationService.Verify(x => x.GoToAsync($"{nameof(RegistrationPage)}"), Times.Once))
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
                .ThenAssert(() => this.tagProfile.Verify(x => x.RevokeLegalIdentity(It.IsAny<LegalIdentity>()), Times.Once))
                .ThenAssert(() => this.navigationService.Verify(x => x.GoToAsync($"{nameof(RegistrationPage)}"), Times.Once))
                .ThenAssert(() => this.uiDispatcher.Verify(x => x.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToReportYourLegalIdentityAsCompromized, AppResources.Yes, AppResources.No), Times.Once));
        }
    }
}