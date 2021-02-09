using System.Linq;
using NUnit.Framework;
using Tag.Neuron.Xamarin.UI.Tests.Extensions;
using Tag.Neuron.Xamarin.UI.ViewModels;

namespace Tag.Neuron.Xamarin.UI.Tests.ViewModels
{
    public class BaseViewModelTests : ViewModelTests<TestBaseViewModel>
    {
        protected override TestBaseViewModel AViewModel()
        {
            return new TestBaseViewModel();
        }

        private TestBaseViewModel AViewModelWithChildren()
        {
            TestBaseViewModel viewModel = new TestBaseViewModel();
            viewModel.AddChild(new BaseViewModel());
            return viewModel;
        }

        [Test]
        public void IsNotBound_Initially()
        {
            GivenAViewModel()
                .ThenAssert(vm => !vm.IsBound);
        }

        [Test]
        public void IsNotBound_NorChildren_Initially()
        {
            Given(AViewModelWithChildren)
                .ThenAssert(vm => !vm.IsBound)
                .ThenAssert(vm => vm.Children.All(x => !x.IsBound));
        }

        [Test]
        public void IsBound_IncludingChildren_AfterBind()
        {
            Given(AViewModelWithChildren)
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => vm.IsBound)
                .ThenAssert(vm => vm.Children.All(x => x.IsBound));
        }

        [Test]
        public void IsBound_AfterBind()
        {
            GivenAViewModel()
                .And(async vm => await vm.Bind())
                .ThenAssert(vm => vm.IsBound);
        }

        [Test]
        public void IsNotBound_AfterBindAndThenUnBind()
        {
            GivenAViewModel()
                .And(async vm => await vm.Bind())
                .And(async vm => await vm.Unbind())
                .ThenAssert(vm => !vm.IsBound);
        }

        [Test]
        public void IsNotBound_NorChildren_AfterBindAndThenUnBind()
        {
            Given(AViewModelWithChildren)
                .And(async vm => await vm.Bind())
                .And(async vm => await vm.Unbind())
                .ThenAssert(vm => !vm.IsBound)
                .ThenAssert(vm => vm.Children.All(x => !x.IsBound));
        }
    }

    public sealed class TestBaseViewModel : BaseViewModel
    {
        public void AddChild(BaseViewModel viewModel)
        {
            this.AddChildViewModel(viewModel);
        }
    }
}