using IdApp.ViewModels.Contracts;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
{
    /// <summary>
    /// A page to display when the user is asked to sign data.
    /// </summary>
    [DesignTimeVisible(true)]
    public partial class PetitionSignaturePage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="PetitionSignaturePage"/> class.
        /// </summary>
        public PetitionSignaturePage()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new PetitionSignatureViewModel();
            InitializeComponent();
        }

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns></returns>
        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
    }
}
