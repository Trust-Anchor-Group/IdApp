using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.Views
{
	[DesignTimeVisible(true)]
	public partial class ViewIdentityPage : INotifyPropertyChanged
    {
		private readonly SignaturePetitionEventArgs review;
		private readonly TagProfile tagProfile;
		private readonly INeuronService neuronService;
		private readonly IContractsService contractsService;
		private readonly INavigationService navigationService;
		private readonly bool personal;
		private LegalIdentity identity;

		public ViewIdentityPage()
			: this(null, null)
		{
		}

		public ViewIdentityPage(LegalIdentity identity)
			: this(identity, null)
		{
		}

		public ViewIdentityPage(LegalIdentity identity, SignaturePetitionEventArgs review)
        {
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.contractsService = DependencyService.Resolve<IContractsService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
			this.tagProfile = DependencyService.Resolve<TagProfile>();
			this.identity = identity ?? this.tagProfile.LegalIdentity;
			this.personal = this.tagProfile.LegalIdentity.Id == identity?.Id;
			this.review = review;
			this.contractsService.LegalIdentityChanged += ContractsService_LegalIdentityChanged;
			this.BindingContext = this;
			InitializeComponent();

			byte[] Png = QR.GenerateCodePng(Constants.IoTSchemes.IotId + ":" + this.identity.Id, (int)this.QrCode.WidthRequest, (int)this.QrCode.HeightRequest);
			this.QrCode.Source = ImageSource.FromStream(() => new MemoryStream(Png));
			this.QrCode.IsVisible = true;

			if (!this.personal)
			{
				this.IdentitySection.Remove(this.NetworkView);
				this.ButtonSection.Remove(this.CompromizedCell);
				this.ButtonSection.Remove(this.RevokeCell);
			}

			if (this.review is null)
			{
				this.ButtonSection.Remove(this.CarefulReviewCell);
				this.ButtonSection.Remove(this.ApprovePiiCell);
				this.ButtonSection.Remove(this.PinCell);
				this.ButtonSection.Remove(this.ApproveReviewCell);
				this.ButtonSection.Remove(this.RejectReviewCell);
			}

			this.LoadPhotos();
		}

        public bool ForReview => !(this.review is null);
		public bool ForReviewFirstName => this.ForReview && !string.IsNullOrEmpty(this.FirstName);
		public bool ForReviewMiddleNames => this.ForReview && !string.IsNullOrEmpty(this.MiddleNames);
		public bool ForReviewLastNames => this.ForReview && !string.IsNullOrEmpty(this.LastNames);
		public bool ForReviewPNr => this.ForReview && !string.IsNullOrEmpty(this.PNr);
		public bool ForReviewAddress => this.ForReview && !string.IsNullOrEmpty(this.Address);
		public bool ForReviewAddress2 => this.ForReview && !string.IsNullOrEmpty(this.Address2);
		public bool ForReviewPostalCode => this.ForReview && !string.IsNullOrEmpty(this.PostalCode);
		public bool ForReviewArea => this.ForReview && !string.IsNullOrEmpty(this.Area);
		public bool ForReviewCity => this.ForReview && !string.IsNullOrEmpty(this.City);
		public bool ForReviewRegion => this.ForReview && !string.IsNullOrEmpty(this.Region);
		public bool ForReviewCountry => this.ForReview && !string.IsNullOrEmpty(this.CountryCode);
		public bool NotForReview => (this.review is null);
		public bool IsPersonal => this.personal;
		public bool NotPersonal => !this.personal;
		public bool ForReviewAndPin => !(this.review is null) && tagProfile.UsePin;

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
							KeyValuePair<string, TemporaryFile> P = await this.contractsService.GetContractAttachmentAsync(Attachment.Url, Constants.Timeouts.DownloadFile);

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

		public DateTime Created => this.identity.Created;
		public DateTime? Updated => CheckMin(this.identity.Updated);
		public string LegalId => this.identity.Id;
		public string BareJid => this.personal ? this.neuronService?.BareJId ?? string.Empty : string.Empty;
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
		public string CountryCode => this.identity["COUNTRY"];
		public string Country => ISO_3166_1.ToName(this.CountryCode);
		public bool IsApproved => this.identity.State == IdentityState.Approved;

		private static DateTime? CheckMin(DateTime? TP)
		{
			if (!TP.HasValue || TP.Value == DateTime.MinValue)
				return null;
			else
				return TP;
		}

        private void ContractsService_LegalIdentityChanged(object sender, LegalIdentityChangedEventArgs e)
        {
            Dispatcher.BeginInvokeOnMainThread(() =>
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
            });
        }

		private async void RevokeButton_Clicked(object sender, EventArgs e)
		{
			if (!this.personal)
				return;

			try
			{
				if (!await this.DisplayAlert("Confirm", "Are you sure you want to revoke your legal identity from the application?", "Yes", "Cancel"))
					return;

				LegalIdentity Identity = await this.contractsService.ObsoleteLegalIdentityAsync(this.identity.Id);

				this.identity = Identity;
				this.tagProfile.ClearLegalIdentity();
                await this.navigationService.PopAsync();
			}
			catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
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

				LegalIdentity Identity = await this.contractsService.CompromisedLegalIdentityAsync(this.identity.Id);
				
				this.identity = Identity;
                this.tagProfile.ClearLegalIdentity();
                await this.navigationService.PopAsync();
			}
			catch (Exception ex)
			{
                await this.navigationService.DisplayAlert(ex);
			}
		}

		private async void ApproveReviewButton_Clicked(object sender, EventArgs e)
		{
			if (this.review is null)
				return;

			try
			{
				if ((!string.IsNullOrEmpty(this.FirstName) && !this.FirstNameCheck.IsChecked) ||
					(!string.IsNullOrEmpty(this.MiddleNames) && !this.MiddleNameCheck.IsChecked) ||
					(!string.IsNullOrEmpty(this.LastNames) && !this.LastNameCheck.IsChecked) ||
					(!string.IsNullOrEmpty(this.PNr) && !this.PersonalNumberCheck.IsChecked) ||
					(!string.IsNullOrEmpty(this.Address) && !this.AddressCheck.IsChecked) ||
					(!string.IsNullOrEmpty(this.Address2) && !this.Address2Check.IsChecked) ||
					(!string.IsNullOrEmpty(this.PostalCode) && !this.PostalCodeCheck.IsChecked) ||
					(!string.IsNullOrEmpty(this.Area) && !this.AreaCheck.IsChecked) ||
					(!string.IsNullOrEmpty(this.City) && !this.CityCheck.IsChecked) ||
					(!string.IsNullOrEmpty(this.Region) && !this.RegionCheck.IsChecked) ||
					(!string.IsNullOrEmpty(this.CountryCode) && !this.CountryCheck.IsChecked))
				{
                    await this.navigationService.DisplayAlert("Incomplete", "Please review all information above, and check the corresponding check boxes if the information is correct. This must be done before you can approve the information.");
					return;
				}

				if (!this.CarefulReviewCheck.IsChecked)
				{
                    await this.navigationService.DisplayAlert("Incomplete", "You need to check the box you have carefully reviewed all corresponding information above.");
					return;
				}

				if (!this.ApprovePiiCheck.IsChecked)
				{
                    await this.navigationService.DisplayAlert("Incomplete", "You need to approve to associate your personal information with the identity you review. When third parties review the information in the identity, they will have access to the identity of the reviewers, for transparency.");
					return;
				}

				if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.PIN.Text) != this.tagProfile.PinHash)
				{
                    await this.navigationService.DisplayAlert("Error", "Invalid PIN.");
					return;
				}

				byte[] Signature = await this.contractsService.SignAsync(this.review.ContentToSign);

				await this.contractsService.SendPetitionSignatureResponseAsync(this.review.SignatoryIdentityId, this.review.ContentToSign, 
					Signature, this.review.PetitionId, this.review.RequestorFullJid, true);

                await this.navigationService.PopAsync();
            }
			catch (Exception ex)
			{
                await this.navigationService.DisplayAlert(ex);
			}
		}

		private async void RejectReviewButton_Clicked(object sender, EventArgs e)
		{
			if (this.review is null)
				return;

			try
			{
				await this.contractsService.SendPetitionSignatureResponseAsync(this.review.SignatoryIdentityId,
					this.review.ContentToSign, new byte[0], this.review.PetitionId, this.review.RequestorFullJid, false);

                await this.navigationService.PopAsync();
			}
			catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
			}
		}

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
    }
}
