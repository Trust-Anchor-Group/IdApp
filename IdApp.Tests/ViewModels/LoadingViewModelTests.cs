using System;
using IdApp.ViewModels;
using IdApp.Views;
using IdApp.Views.Registration;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Tests.Extensions;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;

namespace IdApp.Tests.ViewModels
{
    public class LoadingViewModelTests : ViewModelTests<LoadingViewModel>
    {
        private readonly Mock<INeuronService> neuronService;
        private readonly Mock<INeuronContracts> neuronContracts;
        private readonly Mock<IUiDispatcher> uiDispatcher;
        private readonly Mock<ITagProfile> tagProfile;
        private readonly Mock<INavigationService> navigationService;

        public LoadingViewModelTests()
        {
            this.neuronService = new Mock<INeuronService>();
            this.neuronContracts = new Mock<INeuronContracts>();
            this.uiDispatcher = new Mock<IUiDispatcher>();
            this.tagProfile = new Mock<ITagProfile>();
            this.navigationService = new Mock<INavigationService>();
            this.neuronService.Setup(x => x.Contracts).Returns(this.neuronContracts.Object);
        }

        protected override LoadingViewModel AViewModel()
        {
            return new LoadingViewModel(this.neuronService.Object, this.uiDispatcher.Object, this.tagProfile.Object, this.navigationService.Object);
        }

        [Test]
        public void WhenNeuronServiceIsLoaded_AndProfileIsComplete_ItNavigatesToMainPage()
        {
            this.uiDispatcher.Setup(x => x.BeginInvokeOnMainThread(It.IsAny<Action>())).Callback<Action>(x => x());
            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .And(vm => this.tagProfile.Setup(x => x.IsComplete()).Returns(true))
                .And(vm => this.neuronService.Raise(x => x.Loaded += null, new LoadedEventArgs(true)))
                .ThenAssert(() => this.navigationService.Verify(x => x.GoToAsync($"///{nameof(MainPage)}"), Times.Once))
                .Finally(() =>
                {
                    this.neuronService.Reset();
                    this.navigationService.Reset();
                    this.uiDispatcher.Reset();
                });
        }

        [Test]
        public void WhenNeuronServiceIsLoaded_AndProfileIsIncomplete_ItNavigatesToRegistrationPage()
        {
            this.uiDispatcher.Setup(x => x.BeginInvokeOnMainThread(It.IsAny<Action>())).Callback<Action>(x => x());
            Given(AViewModel)
                .And(async vm => await vm.Bind())
                .And(vm => this.tagProfile.Setup(x => x.IsComplete()).Returns(false))
                .And(vm => this.neuronService.Raise(x => x.Loaded += null, new LoadedEventArgs(true)))
                .ThenAssert(() => this.navigationService.Verify(x => x.GoToAsync($"/{nameof(RegistrationPage)}"), Times.Once))
                .Finally(() =>
                {
                    this.neuronService.Reset();
                    this.navigationService.Reset();
                    this.uiDispatcher.Reset();
                });
        }
    }
}