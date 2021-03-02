using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Tag.Neuron.Xamarin.UI.Views
{
    /// <summary>
    ///  A generic UI component to display an image.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImageView
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ImageView"/> class.
        /// </summary>
        public ImageView()
        {
            InitializeComponent();
        }

        public static readonly BindableProperty SourceProperty =
            BindableProperty.Create("Source", typeof(ImageSource), typeof(ImageView), default(ImageSource));

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        private const uint DurationInMs = 300;

        /// <summary>
        /// Shows the stream as a photo in the current view.
        /// </summary>
        /// <param name="stream">The stream representing the image to show.</param>
        public void ShowPhoto(Stream stream)
        {
            if (stream != null)
            {
                this.Source = ImageSource.FromStream(() => stream);
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                await this.PhotoViewer.FadeTo(1d, DurationInMs, Easing.SinIn);
            });
        }

        /// <summary>
        /// Hides the photo from view.
        /// </summary>
        public void HidePhoto()
        {
            this.PhotoViewer.Opacity = 0;
        }
    }
}