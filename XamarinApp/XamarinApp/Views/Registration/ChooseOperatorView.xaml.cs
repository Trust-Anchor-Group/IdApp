using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;
using XamarinApp.ViewModels.Registration;

namespace XamarinApp.Views.Registration
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChooseOperatorView
    {
        public ChooseOperatorView()
        {
            InitializeComponent();
        }

        private void ManualDomainEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetViewModel<ChooseOperatorViewModel>().ManualOperatorCommand.Execute(e.NewTextValue);
        }
    }
}