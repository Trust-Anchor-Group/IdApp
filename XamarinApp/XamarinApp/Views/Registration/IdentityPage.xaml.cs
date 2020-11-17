using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.Views.Registration
{
	[DesignTimeVisible(true)]
	public partial class IdentityPage : INotifyPropertyChanged
	{
        private readonly TagServiceSettings tagSettings;
        private readonly ITagService tagService;

		public IdentityPage()
		{
            InitializeComponent();
            this.tagSettings = DependencyService.Resolve<TagServiceSettings>();
            this.tagService = DependencyService.Resolve<ITagService>();
			this.BindingContext = this;
        }

		protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPhotos();
            tagService.LegalIdentityChanged += TagService_LegalIdentityChanged;
        }

        protected override void OnDisappearing()
        {
            tagService.LegalIdentityChanged -= TagService_LegalIdentityChanged;
            base.OnDisappearing();
        }

        private async Task LoadPhotos()
        {
            if (tagSettings.HasLegalIdentityAttachments)
            {
                int i = this.TableView.Root.IndexOf(this.ButtonSection);
                TableSection PhotoSection = new TableSection();
                this.TableView.Root.Insert(i++, PhotoSection);

                foreach (Attachment Attachment in tagSettings.GetLegalIdentityAttachments().GetImageAttachments())
                {
                    ViewCell ViewCell;

                    try
                    {
                        KeyValuePair<string, TemporaryFile> P = await tagService.GetContractAttachmentAsync(Attachment.Url, TimeSpan.FromSeconds(10));

						using (TemporaryFile File = P.Value)
                        {
                            MemoryStream ms = new MemoryStream();

                            File.Position = 0;
                            await File.CopyToAsync(ms);
                            ms.Position = 0;

                            ViewCell = new ViewCell()
                            {
                                View = new Image()
                                {
                                    Source = ImageSource.FromStream(() => ms)
                                }
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        ViewCell = new ViewCell()
                        {
                            View = new Label()
                            {
                                Text = ex.Message
                            }
                        };
                    }

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        PhotoSection.Add(ViewCell);
                    });
                }
            }
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

        public DateTime Created => this.tagSettings.LegalIdentity.Created;
        public DateTime? Updated => CheckMin(this.tagSettings.LegalIdentity.Updated);
        public string LegalId => this.tagSettings.LegalIdentity.Id;
        public string BareJid => this.tagService?.BareJID ?? string.Empty;
        public string State => this.tagSettings.LegalIdentity.State.ToString();
        public DateTime? From => CheckMin(this.tagSettings.LegalIdentity.From);
        public DateTime? To => CheckMin(this.tagSettings.LegalIdentity.To);
        public string FirstName => this.tagSettings.LegalIdentity["FIRST"];
        public string MiddleNames => this.tagSettings.LegalIdentity["MIDDLE"];
        public string LastNames => this.tagSettings.LegalIdentity["LAST"];
        public string PNr => this.tagSettings.LegalIdentity["PNR"];
        public string Address => this.tagSettings.LegalIdentity["ADDR"];
        public string Address2 => this.tagSettings.LegalIdentity["ADDR2"];
        public string PostalCode => this.tagSettings.LegalIdentity["ZIP"];
        public string Area => this.tagSettings.LegalIdentity["AREA"];
        public string City => this.tagSettings.LegalIdentity["CITY"];
        public string Region => this.tagSettings.LegalIdentity["REGION"];
        public string Country => this.tagSettings.LegalIdentity["COUNTRY"];
        public bool IsApproved => this.tagSettings.LegalIdentity.State == IdentityState.Approved;

		private static DateTime? CheckMin(DateTime? TP)
		{
			if (!TP.HasValue || TP.Value == DateTime.MinValue)
				return null;
			else
				return TP;
		}

		private async void ContinueButton_Clicked(object sender, EventArgs e)
		{
			try
			{
                this.tagSettings.IncrementConfigurationStep();

                await App.ShowPage();
			}
			catch (Exception ex)
			{
				await this.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
		}

        private void TagService_LegalIdentityChanged(object sender, LegalIdentityChangedEventArgs e)
		{
			this.OnPropertyChanged("Created");
			this.OnPropertyChanged("Updated");
			this.OnPropertyChanged("LegalId");
			this.OnPropertyChanged("BareJid");
			this.OnPropertyChanged("State");
			this.OnPropertyChanged("From");
			this.OnPropertyChanged("To");
			this.OnPropertyChanged("FirstName");
			this.OnPropertyChanged("MiddleNames");
			this.OnPropertyChanged("LastNames");
			this.OnPropertyChanged("PNr");
			this.OnPropertyChanged("Address");
			this.OnPropertyChanged("Address2");
			this.OnPropertyChanged("PostalCode");
			this.OnPropertyChanged("Area");
			this.OnPropertyChanged("City");
			this.OnPropertyChanged("Region");
			this.OnPropertyChanged("Country");
			this.OnPropertyChanged("IsApproved");
		}

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, EventArgs.Empty);
			return true;
		}

        private async void InviteReviewerButton_Clicked(object sender, EventArgs e)
        {
            ScanQrCodePage Dialog = new ScanQrCodePage(this, true);
            Dialog.CodeScanned += async (sender2, e2) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(Dialog.Result))
                    {
                        string Code = Dialog.Result;

                        if (!Code.StartsWith(Constants.Schemes.IotId + ":", StringComparison.InvariantCultureIgnoreCase))
                            throw new Exception("Not a Legal Identity.");

                        await this.tagService.PetitionPeerReviewIDAsync(Code.Substring(6), this.tagSettings.LegalIdentity,
                            Guid.NewGuid().ToString(), "Could you please review my identity information?");

                        Device.BeginInvokeOnMainThread(() =>
                            this.DisplayAlert("Petition sent", "A petition has been sent to your peer.", AppResources.Ok));
                    }
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() => this.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok));
                }
            };

            await this.Navigation.PushModalAsync(Dialog);
        }
    }
}
