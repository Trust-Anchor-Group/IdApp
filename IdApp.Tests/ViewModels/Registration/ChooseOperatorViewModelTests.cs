using IdApp.ViewModels.Registration;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Tests.Extensions;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;

namespace IdApp.Tests.ViewModels.Registration
{
    public class ChooseOperatorViewModelTests : ViewModelTests<ChooseOperatorViewModel>
    {
        private readonly Mock<IUiDispatcher> dispatcher = new Mock<IUiDispatcher>();
        private readonly Mock<INeuronService> neuronService = new Mock<INeuronService>();
        private readonly Mock<INavigationService> navigationService = new Mock<INavigationService>();
        private readonly Mock<INeuronContracts> contracts = new Mock<INeuronContracts>();
        private readonly Mock<ISettingsService> settingsService = new Mock<ISettingsService>();
        private readonly Mock<INetworkService> networkService = new Mock<INetworkService>();
        private readonly Mock<ILogService> logService = new Mock<ILogService>();
        
        public ChooseOperatorViewModelTests()
        {
            this.neuronService.Setup(x => x.Contracts).Returns(contracts.Object);
        }

        protected override ChooseOperatorViewModel AViewModel()
        {
            return new ChooseOperatorViewModel(new TagProfile(), dispatcher.Object, neuronService.Object, navigationService.Object, this.settingsService.Object, this.networkService.Object, this.logService.Object);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void When_OperatorIsNull_ThenButtonIsDisabled(string selectedOperator)
        {
            Given(AViewModel)
                .And(vm =>
                {
                    vm.Operators.Add("Foo");
                    vm.Operators.Add("Bar");
                })
                .And(vm => vm.SelectedOperator = selectedOperator)
                .ThenAssert(vm => vm.ConnectCommand.IsDisabled());
        }

        [Test]
        public void When_OperatorIsSelected_ThenButtonIsEnabled()
        {
            Given(AViewModel)
                .And(vm =>
                {
                    vm.Operators.Add("Foo");
                    vm.Operators.Add("Bar");
                })
                .And(vm => vm.SelectedOperator = "Foo")
                .ThenAssert(vm => vm.ConnectCommand.IsEnabled());
        }

        [Test]
        public void When_ManualOperatorIsSet_ThenButtonIsEnabled()
        {
            Given(AViewModel)
                .And(vm =>
                {
                    vm.Operators.Add("Foo");
                    vm.Operators.Add("Bar");
                })
                .And(vm => vm.SelectedOperator = null)
                .And(vm => vm.ManualOperator = "Baz")
                .ThenAssert(vm => vm.ConnectCommand.IsEnabled());
        }
    }
}
