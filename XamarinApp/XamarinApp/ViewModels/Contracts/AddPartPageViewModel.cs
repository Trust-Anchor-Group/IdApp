using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using XamarinApp.Services;
using XamarinApp.Views.Contracts;

namespace XamarinApp.ViewModels.Contracts
{
    public class AddPartPageViewModel : BaseViewModel
    {
        private readonly IMessageService messageService;
        private readonly INavigationService navigationService;
        private readonly StringEventHandler callback;

        public AddPartPageViewModel(StringEventHandler Callback)
        {
            SwitchModeCommand = new Command(SwitchMode);
            ManualAddCommand = new Command(async () => await PerformManualAdd());
            this.messageService = DependencyService.Resolve<IMessageService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.callback = Callback;
            SetModeText();
        }

        public ICommand SwitchModeCommand { get; }

        public ICommand ManualAddCommand { get; }

        public static readonly BindableProperty LinkTextProperty =
            BindableProperty.Create("LinkText", typeof(string), typeof(AddPartPageViewModel), default(string));

        public string LinkText
        {
            get { return (string)GetValue(LinkTextProperty); }
            set { SetValue(LinkTextProperty, value); }
        }

        public static readonly BindableProperty ScanIsAutomaticProperty =
            BindableProperty.Create("ScanIsAutomatic", typeof(bool), typeof(AddPartPageViewModel), true, propertyChanged: (b, oldValue, newValue) =>
            {
                AddPartPageViewModel viewModel = (AddPartPageViewModel)b;
                viewModel.ScanIsManual = !(bool)newValue;
                viewModel.SetModeText();
            });

        private void SetModeText()
        {
            ModeText = ScanIsAutomatic ? AppResources.QrScanCodeText : AppResources.QrEnterManuallyText;
        }

        public bool ScanIsAutomatic
        {
            get { return (bool)GetValue(ScanIsAutomaticProperty); }
            set { SetValue(ScanIsAutomaticProperty, value); }
        }

        public static readonly BindableProperty ScanIsManualProperty =
            BindableProperty.Create("ScanIsManual", typeof(bool), typeof(AddPartPageViewModel), false);

        public bool ScanIsManual
        {
            get { return (bool)GetValue(ScanIsManualProperty); }
            set { SetValue(ScanIsManualProperty, value); }
        }

        public static readonly BindableProperty ModeTextProperty =
            BindableProperty.Create("ModeText", typeof(string), typeof(AddPartPageViewModel), default(string));

        public string ModeText
        {
            get { return (string)GetValue(ModeTextProperty); }
            set { SetValue(ModeTextProperty, value); }
        }

        private async Task PerformManualScan()
        {
            try
            {
                string Code = this.LinkText;
                int i = Code.IndexOf(':');

                if (i > 0)
                {
                    if (Code.Substring(0, i).ToLower() != Constants.Schemes.IotId)
                    {
                        await this.messageService.DisplayAlert(AppResources.ErrorTitleText, "Not a legal identity.", AppResources.OkButtonText);
                        return;
                    }

                    Code = Code.Substring(i + 1);
                }

                this.callback?.Invoke(Code);
                App.ShowPage(this.owner, true);
            }
            catch (Exception ex)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitleText, ex.Message, AppResources.OkButtonText);
            }

        }

        private void SwitchMode()
        {
            ScanIsAutomatic = !ScanIsAutomatic;
        }

        private async Task PerformManualAdd()
        {
            try
            {
                string Code = LinkText;
                int i = Code.IndexOf(':');

                if (i > 0)
                {
                    if (Code.Substring(0, i).ToLower() != Constants.Schemes.IotId)
                    {
                        await this.messageService.DisplayAlert(AppResources.ErrorTitleText, "Not a legal identity.", AppResources.OkButtonText);
                        return;
                    }

                    Code = Code.Substring(i + 1);
                }

                this.callback?.Invoke(Code);
                App.ShowPage(this.owner, true);
            }
            catch (Exception ex)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitleText, ex.Message, AppResources.OkButtonText);
            }

        }
    }
}