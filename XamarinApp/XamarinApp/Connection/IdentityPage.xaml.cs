using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using XamarinApp.Services;

namespace XamarinApp.Connection
{
	[DesignTimeVisible(true)]
	public partial class IdentityPage : ContentPage, INotifyPropertyChanged
	{
        private readonly ITagService tagService;

		public IdentityPage()
		{
            InitializeComponent();
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
            if (tagService.HasLegalIdentityAttachments)
            {
                int i = this.TableView.Root.IndexOf(this.ButtonSection);
                TableSection PhotoSection = new TableSection();
                this.TableView.Root.Insert(i++, PhotoSection);

                foreach (Attachment Attachment in tagService.GetLegalIdentityAttachments().Where(x => x.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
                {
                    ViewCell ViewCell;

                    try
                    {
                        KeyValuePair<string, TemporaryFile> P = await tagService.GetAttachmentAsync(Attachment.Url, TimeSpan.FromSeconds(10));

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

        public DateTime Created => this.tagService.Configuration.LegalIdentity.Created;
        public DateTime? Updated => CheckMin(this.tagService.Configuration.LegalIdentity.Updated);
        public string LegalId => this.tagService.Configuration.LegalIdentity.Id;
        public string BareJid => this.tagService.Xmpp?.BareJID ?? string.Empty;
        public string State => this.tagService.Configuration.LegalIdentity.State.ToString();
        public DateTime? From => CheckMin(this.tagService.Configuration.LegalIdentity.From);
        public DateTime? To => CheckMin(this.tagService.Configuration.LegalIdentity.To);
        public string FirstName => this.tagService.Configuration.LegalIdentity["FIRST"];
        public string MiddleNames => this.tagService.Configuration.LegalIdentity["MIDDLE"];
        public string LastNames => this.tagService.Configuration.LegalIdentity["LAST"];
        public string PNr => this.tagService.Configuration.LegalIdentity["PNR"];
        public string Address => this.tagService.Configuration.LegalIdentity["ADDR"];
        public string Address2 => this.tagService.Configuration.LegalIdentity["ADDR2"];
        public string PostalCode => this.tagService.Configuration.LegalIdentity["ZIP"];
        public string Area => this.tagService.Configuration.LegalIdentity["AREA"];
        public string City => this.tagService.Configuration.LegalIdentity["CITY"];
        public string Region => this.tagService.Configuration.LegalIdentity["REGION"];
        public string Country => this.tagService.Configuration.LegalIdentity["COUNTRY"];
        public bool IsApproved => this.tagService.Configuration.LegalIdentity.State == IdentityState.Approved;

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
                this.tagService.Configuration.Step++;
                this.tagService.UpdateConfiguration();

                await App.ShowPage();
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
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
			this.BackButton_Clicked(this, new EventArgs());
			return true;
		}
	}
}
