using System;
using IdApp.ViewModels;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Tests.Extensions;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;
using Waher.Networking.XMPP;

namespace IdApp.Tests.ViewModels
{
    public class NeuronViewModelTests : ViewModelTests<NeuronViewModelTests.TestNeuronViewModel>
    {
        private readonly Mock<INeuronService> neuronService;
        private readonly Mock<IUiDispatcher> uiDispatcher;

        public class TestNeuronViewModel : NeuronViewModel
        {
            public TestNeuronViewModel(INeuronService neuronService, IUiDispatcher uiDispatcher)
                : base(neuronService, uiDispatcher)
            {
            }
        }

        public NeuronViewModelTests()
        {
            this.neuronService = new Mock<INeuronService>();
            this.uiDispatcher = new Mock<IUiDispatcher>();
        }

        protected override TestNeuronViewModel AViewModel()
        {
            return new TestNeuronViewModel(this.neuronService.Object, this.uiDispatcher.Object);
        }

        [Test]
        public void ConnectionStateText_IsSetToOffline()
        {
            GivenAViewModel()
                .ThenAssert(vm => vm.ConnectionStateText == AppResources.XmppState_Offline);
        }

        [Test]
        public void ConnectionStateText_ReflectsNeuronServiceState_WhenBound()
        {
            this.neuronService.Setup(x => x.State).Returns(XmppState.Authenticating);
            GivenAViewModel()
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => vm.ConnectionStateText == AppResources.XmppState_Authenticating);
        }

        [Test]
        public void ConnectionStateText_ReflectsNeuronServiceState_WhenStateChanges()
        {
            this.neuronService.SetupGet(x => x.State).Returns(XmppState.Authenticating);
            this.uiDispatcher.Setup(x => x.BeginInvokeOnMainThread(It.IsAny<Action>())).Callback<Action>(x => x());
            GivenAViewModel()
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => vm.ConnectionStateText == AppResources.XmppState_Authenticating)
                .When(vm => this.neuronService.Raise(x => x.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(XmppState.Connected, false)))
                .ThenAssert(vm => vm.ConnectionStateText == AppResources.XmppState_Connected);
        }
    }
}