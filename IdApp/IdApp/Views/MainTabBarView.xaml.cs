using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
    /// <summary>
    /// Represents the main tab bar in the Main page of the application.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainTabBarView
    {
        private const uint DurationInMs = 300;

        private bool isShowing;

        /// <summary>
        /// Creates a new instance of the <see cref="MainTabBarView"/> class.
        /// </summary>
        public MainTabBarView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Call this method to show the toolbar content.
        /// </summary>
        /// <returns></returns>
        public async Task Show()
        {
            if (!this.isShowing)
            {
                this.isShowing = true;
                Task translateButtonTask = this.ScanButton.TranslateTo(0, 30, DurationInMs, Easing.SinOut);
                Task translateToolBarTask = this.ToolBarContent.TranslateTo(0, this.MainToolBar.Height, DurationInMs, Easing.SinOut);
                await Task.WhenAll(translateButtonTask, translateToolBarTask);
            }
        }

        /// <summary>
        /// Call this method to hide the toolbar content.
        /// </summary>
        /// <returns></returns>
        public async Task Hide()
        {
            if (this.isShowing)
            {
                Task translateToolBarTask = this.ToolBarContent.TranslateTo(0, 0, DurationInMs, Easing.SinIn);
                Task translateButtonTask = this.ScanButton.TranslateTo(0, 0, DurationInMs * 2, Easing.SinIn);
                await Task.WhenAll(translateToolBarTask, translateButtonTask);
                this.isShowing = false;
            }
        }
    }
}