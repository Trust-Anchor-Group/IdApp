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
                    this.tagService.Configuration.Step--;
                    this.tagService.UpdateConfiguration();
                }

                await App.ShowPage();
            }
            catch (Exception ex)
            {
                await this.DisplayAlert("Error", ex.Message, "OK");
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
                    await this.DisplayAlert("Error", "PIN number too short. At least 8 numbers (or characters) are required.", "OK");
                    Device.BeginInvokeOnMainThread(() => this.Pin.Focus());
                }
                else if (this.Pin.Text.Trim() != this.Pin.Text)
                {
                    await this.DisplayAlert("Error", "PIN number must not unclude leading or trailing white-space.", "OK");
                    Device.BeginInvokeOnMainThread(() => this.Pin.Focus());
                }
                else if (this.Pin.Text != this.RetypePin.Text)
                {
                    await this.DisplayAlert("Error", "PIN numbers (or passwords) do not match.", "OK");
                    Device.BeginInvokeOnMainThread(() => this.Pin.Focus());
                }
                else
                {
                    this.tagService.Configuration.Pin = this.Pin.Text;
                    this.tagService.Configuration.UsePin = true;

                    if (this.tagService.Configuration.Step == 4)
                        this.tagService.Configuration.Step++;

                    this.tagService.UpdateConfiguration();

                    await App.ShowPage();
                }
            }
            catch (Exception ex)
            {
                await this.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void SkipButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                this.tagService.Configuration.Pin = string.Empty;
                this.tagService.Configuration.UsePin = false;

                if (this.tagService.Configuration.Step == 4)
                    this.tagService.Configuration.Step++;

                this.tagService.UpdateConfiguration();

                await App.ShowPage();
            }
            catch (Exception ex)
            {
                await this.DisplayAlert("Error", ex.Message, "OK");
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
            this.BackButton_Clicked(this, new EventArgs());
            return true;
        }

    }
}
