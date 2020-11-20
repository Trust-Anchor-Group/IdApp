using System.Windows.Input;
using XamarinApp.ViewModels;

namespace XamarinApp.Tests.ViewModels
{
    public abstract class ViewModelTests<T> where T : BaseViewModel
    {
        protected T GivenAViewModel()
        {
            return AViewModel();
        }

        protected abstract T AViewModel();

        protected bool ActionCommandIsDisabled(ICommand command)
        {
            return command != null && command.CanExecute(null) == false;
        }
    }
}
