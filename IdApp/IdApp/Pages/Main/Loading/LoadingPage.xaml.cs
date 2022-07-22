using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Main.Loading
{
    /// <summary>
    /// A page to use when the application is loading, or initializing.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoadingPage : ContentBasePage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="LoadingPage"/> class.
        /// </summary>
        public LoadingPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            this.ViewModel = new LoadingViewModel();
            this.InitializeComponent();
        }

        /// <summary>
        /// Overridden to start an animation when the page is displayed on screen.
        /// </summary>
        protected override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();
            await LabelLayout.FadeTo(1.0, 2000, Easing.CubicInOut);
        }
    }
}
