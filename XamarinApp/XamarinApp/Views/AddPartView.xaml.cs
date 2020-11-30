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
        public event EventHandler<CodeScannedEventArgs> CodeScanned;
        
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

        protected virtual void OnCodeScanned(CodeScannedEventArgs e)
        {
            this.CodeScanned?.Invoke(this, e);
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
            OnCodeScanned(new CodeScannedEventArgs(GetViewModel<AddPartViewModel>().Code));
        }

        private void Scanner_OnScanResult(Result result)
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                GetViewModel<AddPartViewModel>().AutomaticAddCommand.Execute(result.Text);
            }
        }

        public string ScannedCode => GetViewModel<AddPartViewModel>().Code;
    }
}