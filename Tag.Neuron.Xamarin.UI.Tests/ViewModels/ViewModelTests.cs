using System;
using System.Windows.Input;
using Tag.Neuron.Xamarin.UI.ViewModels;

namespace Tag.Neuron.Xamarin.UI.Tests.ViewModels
{
    public abstract class ViewModelTests<T> where T : BaseViewModel
    {
        protected T GivenAViewModel()
        {
            return AViewModel();
        }

        protected abstract T AViewModel();

        protected T Given(Func<T> func)
        {
            return func();
        }

        protected bool ActionCommandIsDisabled(ICommand command)
        {
            return command != null && command.CanExecute(null) == false;
        }

        protected void ActionCommandIsExecuted(ICommand command)
        {
            command.Execute(null);
        }
    }
}
