using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.Views.Registration
{
	[DesignTimeVisible(true)]
	public partial class RegisterIdentityPage : ContentPage, IBackButton
    {
        private readonly ITagService tagService;
		private readonly Dictionary<string, (string, string, byte[])> photos = new Dictionary<string, (string, string, byte[])>();

		public RegisterIdentityPage()
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

		private void FirstName_Completed(object sender, EventArgs e)
		{
			this.MiddleNamesEntry.Focus();
		}

		private void MiddleNames_Completed(object sender, EventArgs e)
		{
			this.LastNamesEntry.Focus();
		}

		private void LastNames_Completed(object sender, EventArgs e)
		{
			this.PNrEntry.Focus();
		}

		private void PNr_Completed(object sender, EventArgs e)
		{
			this.AddressEntry.Focus();
		}

		private void Address_Completed(object sender, EventArgs e)
		{
			this.Address2Entry.Focus();
		}

		private void Address2_Completed(object sender, EventArgs e)
		{
			this.PostalCodeEntry.Focus();
		}

		private void PostalCode_Completed(object sender, EventArgs e)
		{
			this.AreaEntry.Focus();
		}

		private void Area_Completed(object sender, EventArgs e)
		{
			this.CityEntry.Focus();
		}

		private void City_Completed(object sender, EventArgs e)
		{
			this.RegionEntry.Focus();
		}

		private void Region_Completed(object sender, EventArgs e)
		{
			this.CountryEntry.Focus();
		}

		private void Country_Completed(object sender, EventArgs e)
		{
			this.RegisterButton.Focus();
		}

		private void IsWorking(bool Working)
		{
			bool NotWorking = !Working;

			this.RegisterButton.IsEnabled = NotWorking;
			this.FirstNameEntry.IsEnabled = NotWorking;
			this.MiddleNamesEntry.IsEnabled = NotWorking;
			this.LastNamesEntry.IsEnabled = NotWorking;
			this.PNrEntry.IsEnabled = NotWorking;
			this.AddressEntry.IsEnabled = NotWorking;
			this.Address2Entry.IsEnabled = NotWorking;
			this.PostalCodeEntry.IsEnabled = NotWorking;
			this.AreaEntry.IsEnabled = NotWorking;
			this.CityEntry.IsEnabled = NotWorking;
			this.RegionEntry.IsEnabled = NotWorking;
			this.CountryEntry.IsEnabled = NotWorking;
			this.Connecting.IsVisible = Working;
			this.Connecting.IsRunning = Working;
		}

		private async void RegisterButton_Clicked(object sender, EventArgs e)
		{
			if (!this.tagService.IsOnline)
			{
				await DisplayAlert("Error", "Not connected to the operator.", "OK");
				return;
			}

			if (string.IsNullOrEmpty(this.FirstNameEntry.Text.Trim()))
			{
				await DisplayAlert("Error", "You need to provide a first name.", "OK");
				this.FirstNameEntry.Focus();
				return;
			}

			if (string.IsNullOrEmpty(this.LastNamesEntry.Text.Trim()))
			{
				await DisplayAlert("Error", "You need to provide at least one last name.", "OK");
				this.LastNamesEntry.Focus();
				return;
			}

			if (string.IsNullOrEmpty(this.PNrEntry.Text.Trim()))
			{
				await DisplayAlert("Error", "You need to provide a personal or social security number.", "OK");
				this.PNrEntry.Focus();
				return;
			}

			this.IsWorking(true);

			try
			{
				await this.tagService.CheckServices();

				if (string.IsNullOrEmpty(this.tagService.Configuration.LegalJid))
				{
					await DisplayAlert("Error", "Operator does not support legal identities and smart contracts.", "OK");
					return;
				}

				List<Property> Properties = new List<Property>();
				string s;

				if (!string.IsNullOrEmpty(s = this.FirstNameEntry.Text?.Trim()))
					Properties.Add(new Property("FIRST", s));

				if (!string.IsNullOrEmpty(s = this.MiddleNamesEntry.Text?.Trim()))
					Properties.Add(new Property("MIDDLE", s));

				if (!string.IsNullOrEmpty(s = this.LastNamesEntry.Text?.Trim()))
					Properties.Add(new Property("LAST", s));

				if (!string.IsNullOrEmpty(s = this.PNrEntry.Text?.Trim()))
					Properties.Add(new Property("PNR", s));

				if (!string.IsNullOrEmpty(s = this.AddressEntry.Text?.Trim()))
					Properties.Add(new Property("ADDR", s));

				if (!string.IsNullOrEmpty(s = this.Address2Entry.Text?.Trim()))
					Properties.Add(new Property("ADDR2", s));

				if (!string.IsNullOrEmpty(s = this.PostalCodeEntry.Text?.Trim()))
					Properties.Add(new Property("ZIP", s));

				if (!string.IsNullOrEmpty(s = this.AreaEntry.Text?.Trim()))
					Properties.Add(new Property("AREA", s));

				if (!string.IsNullOrEmpty(s = this.CityEntry.Text?.Trim()))
					Properties.Add(new Property("CITY", s));

				if (!string.IsNullOrEmpty(s = this.RegionEntry.Text?.Trim()))
					Properties.Add(new Property("REGION", s));

				if (!string.IsNullOrEmpty(s = this.CountryEntry.Text?.Trim()))
					Properties.Add(new Property("COUNTRY", s));

				if (!string.IsNullOrEmpty(s = this.DeviceID?.Trim()))
					Properties.Add(new Property("DEVICE_ID", s));

                try
                {
                    await this.tagService.AddLegalIdentity(Properties, this.photos.Values.Select(x => new LegalIdentityAttachment(x.Item1, x.Item2, x.Item3)).ToArray());
                }
                catch (Exception ex)
                {
                    await this.DisplayAlert("Error", "Unable to upload photo: " + ex.Message, "OK");
                }

                await App.ShowPage();

				await App.ShowPage();
			}
			catch (Exception ex)
			{
                await this.DisplayAlert("Error", "Unable to register information with " + this.tagService.Configuration.Domain +
                                                 ":\r\n\r\n" + ex.Message, "OK");
			}
			finally
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					this.IsWorking(false);
				});
			}
		}

        public string FirstName => this.tagService.Configuration.LegalIdentity?["FIRST"] ?? string.Empty;
        public string MiddleNames => this.tagService.Configuration.LegalIdentity?["MIDDLE"] ?? string.Empty;
        public string LastNames => this.tagService.Configuration.LegalIdentity?["LAST"] ?? string.Empty;
        public string PNr => this.tagService.Configuration.LegalIdentity?["PNR"] ?? string.Empty;
        public string Address => this.tagService.Configuration.LegalIdentity?["ADDR"] ?? string.Empty;
        public string Address2 => this.tagService.Configuration.LegalIdentity?["ADDR2"] ?? string.Empty;
        public string PostalCode => this.tagService.Configuration.LegalIdentity?["ZIP"] ?? string.Empty;
        public string Area => this.tagService.Configuration.LegalIdentity?["AREA"] ?? string.Empty;
        public string City => this.tagService.Configuration.LegalIdentity?["CITY"] ?? string.Empty;
        public string Region => this.tagService.Configuration.LegalIdentity?["REGION"] ?? string.Empty;
        public string Country => this.tagService.Configuration.LegalIdentity?["COUNTRY"] ?? string.Empty;

		public string DeviceID
		{
			get
			{
				IDeviceInformation DeviceInfo = DependencyService.Get<IDeviceInformation>();
				return DeviceInfo?.GetDeviceID();
			}
		}


		public bool BackClicked()
		{
			this.BackButton_Clicked(this, new EventArgs());
			return true;
		}

        public bool CanTakePhoto =>
            CrossMedia.IsSupported &&
            CrossMedia.Current.IsCameraAvailable &&
            CrossMedia.Current.IsTakePhotoSupported &&
            this.tagService.FileUploadIsSupported;

		private async void AddPhotoButton_Clicked(object sender, EventArgs e)
		{
			MediaFile Photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
			{
				MaxWidthHeight = 1024,
				CompressionQuality = 100,
				AllowCropping = true,
				ModalPresentationStyle = MediaPickerModalPresentationStyle.FullScreen,
				RotateImage = true,
				SaveMetaData = true,
				Directory = "Photos",
				Name = "Photo.jpg",
				DefaultCamera = CameraDevice.Rear
			});

			if (Photo is null)
				return;

			MemoryStream ms = new MemoryStream();
			using (Stream f = Photo.GetStream())
			{
				f.CopyTo(ms);
			}

			byte[] Bin = ms.ToArray();
			string PhotoId = Guid.NewGuid().ToString();

			if ((long)Bin.Length > this.tagService.Configuration.HttpFileUploadMaxSize.GetValueOrDefault())
			{
				ms.Dispose();
				await this.DisplayAlert("Error", "Photo too large.", "OK");
				return;
			}

			int i = this.RegistrationLayout.Children.IndexOf(this.AddPhoto);

			if (this.RegistrationLayout.Children[i - 1] is Entry)
			{
				Label Label = new Label()
				{
					Text = "Photos:"
				};

				this.RegistrationLayout.Children.Insert(i++, Label);
			}

			Image Image = new Image()
			{
				Source = ImageSource.FromStream(() => Photo.GetStream()),
				StyleId = PhotoId
			};

			this.RegistrationLayout.Children.Insert(i++, Image);
			this.photos[PhotoId] = (Photo.Path, "image/jpeg", Bin);

			Button Button = new Button()
			{
				Text = "Remove Photo"
			};

			Button.Clicked += (sender2, e2) =>
			{
				this.RegistrationLayout.Children.Remove(Image);
				this.RegistrationLayout.Children.Remove(Button);
				this.photos.Remove(PhotoId);
			};

			this.RegistrationLayout.Children.Insert(i, Button);
		}

	}
}
