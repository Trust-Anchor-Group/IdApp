using System;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels;
using ZXing;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddPartView
    {
        public AddPartView()
            : this(null)
        {
        }

        // For unit tests
        protected internal AddPartView(AddPartViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? new AddPartViewModel();
            AddPartViewModel vm = GetViewModel<AddPartViewModel>();
            vm.ModeChanged += ViewModel_ModeChanged;
            vm.CodeScanned += ViewModel_CodeScanned;
        }

        public static readonly BindableProperty CodeScannedCommandProperty =
            BindableProperty.Create("CodeScannedCommand", typeof(ICommand), typeof(AddPartView), default(ICommand), BindingMode.OneWayToSource);

        public ICommand CodeScannedCommand
        {
            get { return (ICommand) GetValue(CodeScannedCommandProperty); }
            set { SetValue(CodeScannedCommandProperty, value); }
        }

        private void ViewModel_ModeChanged(object sender, EventArgs e)
        {
            if (GetViewModel<AddPartViewModel>().ScanIsManual)
            {
                this.LinkEntry.Focus();
            }
        }

        private void ViewModel_CodeScanned(object sender, EventArgs e)
        {
            CodeScannedCommand?.Execute(GetViewModel<AddPartViewModel>().Code);
        }

        private void Scanner_OnScanResult(Result result)
        {
            GetViewModel<AddPartViewModel>().AutomaticAddCommand.Execute(result.Text);
        }
    }
}