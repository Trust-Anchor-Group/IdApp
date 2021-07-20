using System;
using System.Threading.Tasks;
using IdApp.Pages.Registration.Registration;
using IdApp.Services;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Tests.Services
{
	public class ContractOrchestratorServiceTests
	{
		private readonly Mock<ITagProfile> tagProfile;
		private readonly Mock<IUiDispatcher> uiDispatcher;
		private readonly Mock<INeuronService> neuronService;
		private readonly Mock<INeuronContracts> neuronContracts;
		private readonly Mock<INetworkService> networkService;
		private readonly Mock<INavigationService> navigationService;
		private readonly Mock<ILogService> logService;
		private readonly Mock<ISettingsService> settingsService;
		private readonly TestContractOrchestratorService sut;

		private class TestContractOrchestratorService : ContractOrchestratorService
		{
			public TestContractOrchestratorService(ITagProfile tagProfile, IUiDispatcher uiDispatcher, INeuronService neuronService, INavigationService navigationService, ILogService logService, INetworkService networkService, ISettingsService settingsService)
			: base(tagProfile, uiDispatcher, neuronService, navigationService, logService, networkService, settingsService)
			{
			}

			public int DownloadCount { get; private set; }

			protected override void DownloadLegalIdentityInternal(string legalId)
			{
				DownloadCount++;
			}

			public async Task PerformDownloadOfLegalIdentity(string legalId)
			{
				await this.DownloadLegalIdentity(legalId);
			}
		}

		public ContractOrchestratorServiceTests()
		{
			this.tagProfile = new Mock<ITagProfile>();
			this.tagProfile.Setup(x => x.Domains).Returns(new[] { "Foo" });
			this.neuronService = new Mock<INeuronService>();
			this.neuronContracts = new Mock<INeuronContracts>();
			this.networkService = new Mock<INetworkService>();
			this.navigationService = new Mock<INavigationService>();
			this.neuronService.SetupGet(x => x.Contracts).Returns(this.neuronContracts.Object);
			this.logService = new Mock<ILogService>();
			this.settingsService = new Mock<ISettingsService>();
			this.uiDispatcher = new Mock<IUiDispatcher>();
			this.sut = new TestContractOrchestratorService(this.tagProfile.Object, this.uiDispatcher.Object, this.neuronService.Object, this.navigationService.Object, this.logService.Object, this.networkService.Object, this.settingsService.Object);
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
			this.tagProfile.Reset();
			this.neuronContracts.Reset();
			this.networkService.Reset();
			this.navigationService.Reset();
		}

		[Test]
		public async Task DownloadsLegalIdentity_WhenIdentityIsSet_AndStateIsCompleteOrWaitingForValidation()
		{
			Guid guid = Guid.NewGuid();
			this.neuronService.SetupGet(x => x.IsOnline).Returns(true);
			this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(new LegalIdentity { Id = guid.ToString() });
			this.tagProfile.Setup(x => x.IsCompleteOrWaitingForValidation()).Returns(true);
			await Task.Delay(TimeSpan.FromSeconds(2));
			Assert.AreEqual(1, this.sut.DownloadCount);
		}

		[Test]
		public void DoesNotDownloadLegalIdentity_WhenIdentityIsMissing_AndStateIsCompleteOrWaitingForValidation()
		{
			this.neuronService.SetupGet(x => x.IsOnline).Returns(true);
			this.tagProfile.Setup(x => x.IsCompleteOrWaitingForValidation()).Returns(true);
			Assert.AreEqual(0, this.sut.DownloadCount);
		}

		[Test]
		public void DoesNotDownloadLegalIdentity_WhenIdentityIsSet_AndStateIsIncorrect()
		{
			this.neuronService.SetupGet(x => x.IsOnline).Returns(true);
			Guid guid = Guid.NewGuid();
			this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(new LegalIdentity { Id = guid.ToString() });
			this.tagProfile.Setup(x => x.IsCompleteOrWaitingForValidation()).Returns(false);
			Assert.AreEqual(0, this.sut.DownloadCount);
		}

		[Test]
		public async Task WhenDownloadLegalIdentity_AndIdentityIsCompromised_UserIsRedirectsToRegistrationPage()
		{
			// Given
			Guid guid = Guid.NewGuid();
			LegalIdentity identity = new LegalIdentity { Id = guid.ToString(), State = IdentityState.Approved };
			LegalIdentity compromisedIdentity = new LegalIdentity { Id = guid.ToString(), State = IdentityState.Compromised };
			this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(identity);
			this.tagProfile.Setup(x => x.IsCompleteOrWaitingForValidation()).Returns(true);
			this.uiDispatcher.Setup(x => x.BeginInvokeOnMainThread(It.IsAny<Action>())).Callback<Action>(x => x());
			this.networkService.Setup(x => x.TryRequest(It.IsAny<Func<Task<LegalIdentity>>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
				.Returns(Task.FromResult((true, compromisedIdentity)));
			this.neuronService.Setup(x => x.WaitForConnectedState(It.IsAny<TimeSpan>())).Returns(Task.FromResult(true));
			this.neuronContracts.Setup(x => x.GetLegalIdentity(It.IsAny<string>())).Returns(Task.FromResult(compromisedIdentity));
			this.neuronService.SetupGet(x => x.IsOnline).Returns(true);
			// When
			await this.sut.PerformDownloadOfLegalIdentity(guid.ToString());
			// Then
			this.tagProfile.Verify(x => x.CompromiseLegalIdentity(It.Is<LegalIdentity>(id => id.State == IdentityState.Compromised)), Times.Once);
			this.navigationService.Verify(x => x.GoToAsync(nameof(RegistrationPage)), Times.Once);
		}

		[Test]
		public async Task WhenDownloadLegalIdentity_AndIdentityIsObsolete_UserIsRedirectsToRegistrationPage()
		{
			// Given
			Guid guid = Guid.NewGuid();
			LegalIdentity identity = new LegalIdentity { Id = guid.ToString(), State = IdentityState.Approved };
			LegalIdentity compromisedIdentity = new LegalIdentity { Id = guid.ToString(), State = IdentityState.Compromised };
			this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(identity);
			this.tagProfile.Setup(x => x.IsCompleteOrWaitingForValidation()).Returns(true);
			this.uiDispatcher.Setup(x => x.BeginInvokeOnMainThread(It.IsAny<Action>())).Callback<Action>(x => x());
			this.networkService.Setup(x => x.TryRequest(It.IsAny<Func<Task<LegalIdentity>>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
				.Returns(Task.FromResult((true, compromisedIdentity)));
			this.neuronService.Setup(x => x.WaitForConnectedState(It.IsAny<TimeSpan>())).Returns(Task.FromResult(true));
			this.neuronContracts.Setup(x => x.GetLegalIdentity(It.IsAny<string>())).Returns(Task.FromResult(compromisedIdentity));
			this.neuronService.SetupGet(x => x.IsOnline).Returns(true);
			// When
			await this.sut.PerformDownloadOfLegalIdentity(guid.ToString());
			// Then
			this.tagProfile.Verify(x => x.CompromiseLegalIdentity(It.IsAny<LegalIdentity>()), Times.Once);
			this.navigationService.Verify(x => x.GoToAsync(nameof(RegistrationPage)), Times.Once);
		}

		[Test]
		public async Task WhenDownloadLegalIdentity_AndPrivateKeysAreInvalid_UserIsRedirectsToRegistrationPage()
		{
			// Given
			Guid guid = Guid.NewGuid();
			LegalIdentity identity = new LegalIdentity { Id = guid.ToString(), State = IdentityState.Approved };
			LegalIdentity compromisedIdentity = new LegalIdentity { Id = guid.ToString(), State = IdentityState.Obsoleted };
			this.tagProfile.SetupGet(x => x.LegalIdentity).Returns(identity);
			this.tagProfile.Setup(x => x.IsCompleteOrWaitingForValidation()).Returns(true);
			this.uiDispatcher.Setup(x => x.BeginInvokeOnMainThread(It.IsAny<Action>())).Callback<Action>(x => x());
			this.networkService.Setup(x => x.TryRequest(It.IsAny<Func<Task<LegalIdentity>>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
				.Returns(Task.FromResult((true, identity)));
			this.neuronService.Setup(x => x.WaitForConnectedState(It.IsAny<TimeSpan>())).Returns(Task.FromResult(true));
			this.neuronContracts.Setup(x => x.GetLegalIdentity(It.IsAny<string>())).Returns(Task.FromResult(identity));
			this.neuronContracts.Setup(x => x.HasPrivateKey(It.IsAny<string>(), null)).ReturnsAsync(false);
			this.neuronService.SetupGet(x => x.IsOnline).Returns(true);
			this.neuronContracts.Setup(x => x.ObsoleteLegalIdentity(guid.ToString())).ReturnsAsync(compromisedIdentity);
			// When
			await this.sut.PerformDownloadOfLegalIdentity(guid.ToString());
			// Then
			this.tagProfile.Verify(x => x.RevokeLegalIdentity(It.IsAny<LegalIdentity>()), Times.Once);
			this.navigationService.Verify(x => x.GoToAsync(nameof(RegistrationPage)), Times.Once);
			this.neuronContracts.Verify(x => x.HasPrivateKey(guid.ToString(), null), Times.Once);
			this.neuronContracts.Verify(x => x.ObsoleteLegalIdentity(guid.ToString()), Times.Once);
		}
	}
}