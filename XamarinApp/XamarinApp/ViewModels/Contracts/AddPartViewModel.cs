using System.Windows.Input;
using Xamarin.Forms;

namespace XamarinApp.ViewModels.Contracts
{
    public class AddPartViewModel : BaseViewModel
    {
        public AddPartViewModel()
        {
            CodeScannedCommand = new Command<string>(code => Code = code);
        }

        public ICommand CodeScannedCommand { get; }

        public static readonly BindableProperty CodeProperty =
            BindableProperty.Create("Code", typeof(string), typeof(AddPartViewModel), default(string));

        public string Code
        {
            get { return (string)GetValue(CodeProperty); }
            set { SetValue(CodeProperty, value); }
        }
    }
}