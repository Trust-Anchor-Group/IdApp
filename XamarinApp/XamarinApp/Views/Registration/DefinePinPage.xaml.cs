﻿using System;
using System.ComponentModel;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.Views.Registration
{
    [DesignTimeVisible(true)]
    public partial class DefinePinPage : IBackButton
    {
        private readonly TagServiceSettings tagSettings;
        private readonly ITagService tagService;

        public DefinePinPage()
        {
            InitializeComponent();
            this.tagSettings = DependencyService.Resolve<TagServiceSettings>();
            this.tagService = DependencyService.Resolve<ITagService>();
            this.BindingContext = this;
        }

        private async void BackButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (this.tagSettings.Step > 0)
                {
                    this.tagSettings.DecrementConfigurationStep();
                }

                await App.ShowPage();
            }
            catch (Exception ex)
            {
                await this.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
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
                    await this.DisplayAlert(AppResources.ErrorTitle, "PIN number too short. At least 8 numbers (or characters) are required.", AppResources.Ok);
                    Device.BeginInvokeOnMainThread(() => this.Pin.Focus());
                }
                else if (this.Pin.Text.Trim() != this.Pin.Text)
                {
                    await this.DisplayAlert(AppResources.ErrorTitle, "PIN number must not unclude leading or trailing white-space.", AppResources.Ok);
                    Device.BeginInvokeOnMainThread(() => this.Pin.Focus());
                }
                else if (this.Pin.Text != this.RetypePin.Text)
                {
                    await this.DisplayAlert(AppResources.ErrorTitle, "PIN numbers (or passwords) do not match.", AppResources.Ok);
                    Device.BeginInvokeOnMainThread(() => this.Pin.Focus());
                }
                else
                {
                    if (this.tagSettings.Step == 4)
                        this.tagSettings.IncrementConfigurationStep();

                    this.tagSettings.SetPin(this.Pin.Text, true);

                    await App.ShowPage();
                }
            }
            catch (Exception ex)
            {
                await this.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
            }
        }

        private async void SkipButton_Clicked(object sender, EventArgs e)
        {
            try
            {
               
                if (this.tagSettings.Step == 4)
                    this.tagSettings.IncrementConfigurationStep();

                this.tagSettings.ResetPin();

                await App.ShowPage();
            }
            catch (Exception ex)
            {
                await this.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
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
