using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
    [DesignTimeVisible(true)]
    public partial class PetitionIdentityPage
    {
        private readonly LegalIdentity requestorIdentity;
        private readonly string requestorFullJid;
        private readonly string requestedIdentityId;
        private readonly string petitionId;
        private readonly string purpose;
        private readonly INavigationService navigationService;
        private readonly IContractsService contractsService;

        public PetitionIdentityPage(LegalIdentity requestorIdentity, string requestorFullJid,
            string requestedIdentityId, string petitionId, string purpose)
        {
            this.requestorIdentity = requestorIdentity;
            this.requestorFullJid = requestorFullJid;
            this.requestedIdentityId = requestedIdentityId;
            this.petitionId = petitionId;
            this.purpose = purpose;
            this.BindingContext = this;
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.contractsService = DependencyService.Resolve<IContractsService>();
            InitializeComponent();

            this.LoadPhotos();
        }

        private async void LoadPhotos()
        {
            if (!(this.requestorIdentity.Attachments is null))
            {
                int i = this.TableView.Root.IndexOf(this.ButtonSection);
                TableSection photoSection = new TableSection();
                this.TableView.Root.Insert(i++, photoSection);

                foreach (Attachment attachment in this.requestorIdentity.Attachments.GetImageAttachments())
                {
                    ViewCell viewCell;

                    try
                    {
                        KeyValuePair<string, TemporaryFile> p = await this.contractsService.GetContractAttachmentAsync(attachment.Url, Constants.Timeouts.DownloadFile);

                        using (TemporaryFile file = p.Value)
                        {
                            MemoryStream ms = new MemoryStream();

                            file.Position = 0;
                            await file.CopyToAsync(ms);
                            ms.Position = 0;

                            viewCell = new ViewCell()
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
                        viewCell = new ViewCell()
                        {
                            View = new Label()
                            {
                                Text = ex.Message
                            }
                        };
                    }

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        photoSection.Add(viewCell);
                    });
                }
            }
        }

        private async void AcceptButton_Clicked(object sender, EventArgs e)
        {
            await this.contractsService.SendPetitionIdentityResponseAsync(this.requestedIdentityId, this.petitionId, this.requestorFullJid, true);
            await this.navigationService.PopAsync();
        }

        private async void DeclineButton_Clicked(object sender, EventArgs e)
        {
            await this.contractsService.SendPetitionIdentityResponseAsync(this.requestedIdentityId, this.petitionId, this.requestorFullJid, false);
            await this.navigationService.PopAsync();
        }

        private async void IgnoreButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PopAsync();
        }

        public DateTime Created => this.requestorIdentity.Created;
        public DateTime? Updated => CheckMin(this.requestorIdentity.Updated);
        public string LegalId => this.requestorIdentity.Id;
        public string State => this.requestorIdentity.State.ToString();
        public DateTime? From => CheckMin(this.requestorIdentity.From);
        public DateTime? To => CheckMin(this.requestorIdentity.To);
        public string FirstName => this.requestorIdentity["FIRST"];
        public string MiddleNames => this.requestorIdentity["MIDDLE"];
        public string LastNames => this.requestorIdentity["LAST"];
        public string PNr => this.requestorIdentity["PNR"];
        public string Address => this.requestorIdentity["ADDR"];
        public string Address2 => this.requestorIdentity["ADDR2"];
        public string PostalCode => this.requestorIdentity["ZIP"];
        public string Area => this.requestorIdentity["AREA"];
        public string City => this.requestorIdentity["CITY"];
        public string Region => this.requestorIdentity["REGION"];
        public string CountryCode => this.requestorIdentity["COUNTRY"];
        public string Country => ISO_3166_1.ToName(this.CountryCode);
        public bool IsApproved => this.requestorIdentity.State == IdentityState.Approved;
        public string Purpose => this.purpose;

        private static DateTime? CheckMin(DateTime? tp)
        {
            if (!tp.HasValue || tp.Value == DateTime.MinValue)
                return null;
            else
                return tp;
        }

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
    }
}
