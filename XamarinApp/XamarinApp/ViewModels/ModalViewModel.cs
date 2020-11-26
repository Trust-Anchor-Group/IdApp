using System.Windows.Input;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels
{
    public class ModalViewModel : BaseViewModel
    {
        private readonly INavigationService navigationService;

        public ModalViewModel()
        : this(null)
        {
        }

        public ModalViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
            this.GoBackCommand = new Command(_ => GoBack());
        }

        public ICommand GoBackCommand { get; }

        private void GoBack()
        {
            this.navigationService.PopModalAsync();
        }
    }
}