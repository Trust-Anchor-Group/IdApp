using System;
using System.IO;
using System.Threading.Tasks;
using IdApp.Services;
using IdApp.ViewModels.Registration;
using Moq;
using NUnit.Framework;
using SkiaSharp;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Extensions;
using Tag.Neuron.Xamarin.UI.Tests;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Tests.ViewModels.Registration
{
    public class RegisterIdentityViewModelTests : ViewModelTests<RegisterIdentityViewModel>
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

        public RegisterIdentityViewModelTests()
        {
            this.neuronService.Setup(x => x.Contracts).Returns(contracts.Object);
            MockForms.Init();
        }

        protected override RegisterIdentityViewModel AViewModel()
        {
            return new RegisterIdentityViewModel(tagProfile.Object, dispatcher.Object, neuronService.Object, navigationService.Object, this.settingsService.Object, this.networkService.Object, this.logService.Object, this.attachmentCacheService.Object);
        }

        const string Folder = "images";
        const string File = "photo.jpg";
        private string fullPath;
        private LegalIdentity identity;

        private RegisterIdentityViewModel CreateViewModelForRegister(bool photoUploadSucceeds)
        {
            SKBitmap bitmap = new SKBitmap(300, 300);
            string path = Path.Combine(Path.GetTempPath(), Folder);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            this.fullPath = Path.Combine(path, File);
            using (var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100))
            {
                byte[] bytes = data.ToArray();
                System.IO.File.WriteAllBytes(fullPath, bytes);
            }

            this.neuronService.SetupGet(x => x.IsOnline).Returns(true);
            Guid guid = Guid.NewGuid();
            // Upload attachment
            this.identity = new LegalIdentity { Id = guid.ToString(), State = IdentityState.Approved };
            this.networkService.Setup(x => x.TryRequest(It.IsAny<Func<Task<LegalIdentity>>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.FromResult((photoUploadSucceeds, this.identity)));
            // Short circuit the begin invoke calls, so they're executed synchronously.
            this.dispatcher.Setup(x => x.BeginInvokeOnMainThread(It.IsAny<Action>())).Callback<Action>(x => x());
            this.tagProfile.SetupGet(x => x.HttpFileUploadMaxSize).Returns(short.MaxValue);
            this.tagProfile.SetupGet(x => x.LegalJid).Returns(Guid.NewGuid().ToString);
            this.tagProfile.SetupGet(x => x.RegistryJid).Returns(Guid.NewGuid().ToString);
            this.tagProfile.SetupGet(x => x.ProvisioningJid).Returns(Guid.NewGuid().ToString);

            return AViewModel();
        }

        [Test]
        public async Task Register_SetsLegalIdentityOnTagProfile_AndFiresStepCompletedEvent()
        {
            // Given
            RegisterIdentityViewModel vm = CreateViewModelForRegister(true);
            int stepCompletedEventFired = 0;
            vm.StepCompleted += (sender, args) => stepCompletedEventFired++;
            // Set properties
            await vm.AddPhoto(fullPath, false);
            foreach (string country in ISO_3166_1.Countries)
            {
                vm.Countries.Add(country);
            }
            vm.FirstName = "first";
            vm.LastNames = "last";
            vm.Address = "address";
            vm.City = "city";
            vm.SelectedCountry = "SWEDEN";
            vm.PersonalNumber = "191212-1212";
            // When
            vm.RegisterCommand.Execute();
            // Then
            this.tagProfile.Verify(x => x.SetLegalIdentity(identity), Times.Once);
            Assert.IsTrue(stepCompletedEventFired == 1);
        }

        [Test]
        public async Task Register_WhenUploadAttachmentFails_DoesNotSetLegalIdentityOnTagProfile_AndDoesNotFireStepCompletedEvent()
        {
            // Given
            RegisterIdentityViewModel vm = CreateViewModelForRegister(false);
            int stepCompletedEventFired = 0;
            vm.StepCompleted += (sender, args) => stepCompletedEventFired++;
            // Set properties
            await vm.AddPhoto(fullPath, false);
            foreach (string country in ISO_3166_1.Countries)
            {
                vm.Countries.Add(country);
            }
            vm.FirstName = "first";
            vm.LastNames = "last";
            vm.Address = "address";
            vm.City = "city";
            vm.SelectedCountry = "SWEDEN";
            vm.PersonalNumber = "191212-1212";
            // When
            vm.RegisterCommand.Execute();
            // Then
            this.tagProfile.Verify(x => x.SetLegalIdentity(identity), Times.Never);
            Assert.IsTrue(stepCompletedEventFired == 0);
        }
    }
}