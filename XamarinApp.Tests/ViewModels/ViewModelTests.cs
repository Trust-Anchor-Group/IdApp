using System;
using System.Windows.Input;
using Tag.Sdk.UI.ViewModels;

namespace XamarinApp.Tests.ViewModels
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
    }
}
