using System.ComponentModel;
using Xamarin.Forms;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
{
    [DesignTimeVisible(true)]
    public partial class AddPartPage
    {
        private readonly bool isModal;

        public AddPartPage(AddPartViewModel viewModel, Page owner, bool isModal)
        {
            InitializeComponent();
            this.isModal = isModal;
            this.ViewModel = viewModel ?? new AddPartViewModel();
        }

        public AddPartPage(Page owner, bool isModal)
            : this(null, owner, isModal)
        {
        }

        protected override bool OnBackButtonPressed()
        {
            return this.isModal || base.OnBackButtonPressed();
        }
    }
}
