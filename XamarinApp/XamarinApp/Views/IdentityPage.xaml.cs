using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.Views
{
    [DesignTimeVisible(true)]
    public partial class IdentityPage : INotifyPropertyChanged, ILegalIdentityChanged, IBackButton
    {
        private readonly ITagService tagService;
        private readonly Page owner;
        private readonly bool personal;
        private LegalIdentity identity;

        public IdentityPage(Page Owner)
            : this(Owner, App.Instance.TagService.Configuration.LegalIdentity, true)
        {
        }

        public IdentityPage(Page Owner, LegalIdentity Identity)
            : this(Owner, Identity, false)
        {
        }

        private IdentityPage(Page Owner, LegalIdentity Identity, bool Personal)
        {
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
            this.owner = Owner;
            this.identity = Identity;
            this.personal = Personal || this.tagService.Configuration.LegalIdentity.Id == Identity.Id;
            this.BindingContext = this;

            byte[] Png = QR.GenerateCodePng(Constants.Schemes.IotId + ":" + Identity.Id, (int)this.QrCode.WidthRequest, (int)this.QrCode.HeightRequest);
            this.QrCode.Source = ImageSource.FromStream(() => new MemoryStream(Png));
            this.QrCode.IsVisible = true;

            this.CompromizedButton.IsVisible = Personal;
            this.RevokeButton.IsVisible = Personal;

            if (!Personal)
            {
                this.IdentitySection.Remove(this.NetworkView);
                this.ButtonSection.Remove(this.CompromizedButtonView);
                this.ButtonSection.Remove(this.RevokeButtonView);
            }

            this.LoadPhotos();
        }

        private async void LoadPhotos()
        {
            if (!(this.identity.Attachments is null))
            {
                int i = this.TableView.Root.IndexOf(this.ButtonSection);
                TableSection PhotoSection = new TableSection();
                this.TableView.Root.Insert(i++, PhotoSection);

                foreach (Attachment Attachment in this.identity.Attachments.GetImageAttachments())
                {
                    ViewCell ViewCell;

                    try
                    {
                        KeyValuePair<string, TemporaryFile> P = await this.tagService.GetContractAttachmentAsync(Attachment.Url, TimeSpan.FromSeconds(10));

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

        private void BackButton_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(this.owner, true);
        }

        public DateTime Created => this.identity.Created;
        public DateTime? Updated => CheckMin(this.identity.Updated);
        public string LegalId => this.identity.Id;
        public string BareJid => this.personal ? this.tagService.BareJID ?? string.Empty : string.Empty;
        public string State => this.identity.State.ToString();
        public DateTime? From => CheckMin(this.identity.From);
        public DateTime? To => CheckMin(this.identity.To);
        public string FirstName => this.identity["FIRST"];
        public string MiddleNames => this.identity["MIDDLE"];
        public string LastNames => this.identity["LAST"];
        public string PNr => this.identity["PNR"];
        public string Address => this.identity["ADDR"];
        public string Address2 => this.identity["ADDR2"];
        public string PostalCode => this.identity["ZIP"];
        public string Area => this.identity["AREA"];
        public string City => this.identity["CITY"];
        public string Region => this.identity["REGION"];
        public string Country => this.identity["COUNTRY"];
        public bool IsApproved => this.identity.State == IdentityState.Approved;

        private static DateTime? CheckMin(DateTime? TP)
        {
            if (!TP.HasValue || TP.Value == DateTime.MinValue)
                return null;
            else
                return TP;
        }

        public void LegalIdentityChanged(LegalIdentity Identity)
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

        private async void RevokeButton_Clicked(object sender, EventArgs e)
        {
            if (!this.personal)
                return;

            try
            {
                if (!await this.DisplayAlert("Confirm", "Are you sure you want to revoke your legal identity from the application?", "Yes", "Cancel"))
                    return;

                LegalIdentity Identity = await this.tagService.ObsoleteLegalIdentityAsync(this.identity.Id);

                this.identity = Identity;
                this.tagService.DecrementConfigurationStep(2);

                await App.ShowPage();
            }
            catch (Exception ex)
            {
                await this.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
            }
        }

        private async void CompromizedButton_Clicked(object sender, EventArgs e)
        {
            if (!this.personal)
                return;

            try
            {
                if (!await this.DisplayAlert("Confirm", "Are you sure you want to report your legal identity as compromized, stolen or hacked?", "Yes", "Cancel"))
                    return;

                LegalIdentity Identity = await this.tagService.CompromisedLegalIdentityAsync(this.identity.Id);

                this.identity = Identity;
                this.tagService.DecrementConfigurationStep(2);

                await App.ShowPage();
            }
            catch (Exception ex)
            {
                await this.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
            }
        }

        public bool BackClicked()
        {
            this.BackButton_Clicked(this, EventArgs.Empty);
            return true;
        }

    }
}
