using System;
using System.ComponentModel;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.Views.Registration
{
    [DesignTimeVisible(true)]
    public partial class DefinePinPage : ContentPage, IBackButton
    {
        private readonly ITagService tagService;

        public DefinePinPage()
        {
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
            this.BindingContext = this;
        }

        private async void BackButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (this.tagService.Configuration.Step > 0)
                {
                    this.tagService.DecrementConfigurationStep();
                }

                await App.ShowPage();
            }
            catch (Exception ex)
            {
                await this.DisplayAlert(AppResources.ErrorTitleText, ex.Message, AppResources.OkButtonText);
            }
        }

        private void Pin_Completed(object sender, EventArgs e)
        {
            this.RetypePin.Focus();
        }

        private void RetypePin_Completed(object sender, EventArgs e)
        {
            this.ScrollView.ScrollToAsync(this.BackButton, ScrollToPosition.End, false);
        }

        private async void OkButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (this.Pin.Text.Length < 8)
                {
                    await this.DisplayAlert(AppResources.ErrorTitleText, "PIN number too short. At least 8 numbers (or characters) are required.", AppResources.OkButtonText);
                    Device.BeginInvokeOnMainThread(() => this.Pin.Focus());
                }
                else if (this.Pin.Text.Trim() != this.Pin.Text)
                {
                    await this.DisplayAlert(AppResources.ErrorTitleText, "PIN number must not unclude leading or trailing white-space.", AppResources.OkButtonText);
                    Device.BeginInvokeOnMainThread(() => this.Pin.Focus());
                }
                else if (this.Pin.Text != this.RetypePin.Text)
                {
                    await this.DisplayAlert(AppResources.ErrorTitleText, "PIN numbers (or passwords) do not match.", AppResources.OkButtonText);
                    Device.BeginInvokeOnMainThread(() => this.Pin.Focus());
                }
                else
                {
                    if (this.tagService.Configuration.Step == 4)
                        this.tagService.Configuration.Step++;

                    this.tagService.SetPin(this.Pin.Text, true);

                    await App.ShowPage();
                }
            }
            catch (Exception ex)
            {
                await this.DisplayAlert(AppResources.ErrorTitleText, ex.Message, AppResources.OkButtonText);
            }
        }

        private async void SkipButton_Clicked(object sender, EventArgs e)
        {
            try
            {
               
                if (this.tagService.Configuration.Step == 4)
                    this.tagService.Configuration.Step++;

                this.tagService.ResetPin();

                await App.ShowPage();
            }
            catch (Exception ex)
            {
                await this.DisplayAlert(AppResources.ErrorTitleText, ex.Message, AppResources.OkButtonText);
            }
        }

        public bool IsPinOk
        {
            get
            {
                string Pin = this.Pin?.Text ?? string.Empty;
                return Pin.Length >= 8 && Pin == this.RetypePin.Text && Pin.Trim() == Pin;
            }
        }

        private void Pin_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.OnPropertyChanged("IsPinOk");
        }

        public bool BackClicked()
        {
            this.BackButton_Clicked(this, EventArgs.Empty);
            return true;
        }

    }
}
