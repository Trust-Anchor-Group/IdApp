using IdApp.ViewModels.Registration;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Registration
{
    /// <summary>
    /// A view to display the 'choose operator' during the registration process.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChooseOperatorView
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ChooseOperatorView"/> class.
        /// </summary>
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