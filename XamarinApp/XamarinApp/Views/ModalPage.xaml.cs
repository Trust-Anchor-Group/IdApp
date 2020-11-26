using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ModalPage
    {
        public ModalPage()
        {
            InitializeComponent();
            ViewModel = new ModalViewModel();
        }

        public static readonly BindableProperty ModalContentProperty =
            BindableProperty.Create("ModalContent", typeof(View), typeof(ModalPage), default(View));

        public View ModalContent
        {
            get { return (View)GetValue(ModalContentProperty); }
            set { SetValue(ModalContentProperty, value); }
        }

        protected override bool OnBackButtonPressed()
        {
            GetViewModel<ModalViewModel>().GoBackCommand.Execute(null);
            return true;
        }
    }
}