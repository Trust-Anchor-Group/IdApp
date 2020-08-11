using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Xamarin.Forms;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.MainMenu.Contracts
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(true)]
	public partial class PetitionIdentityPage : ContentPage, IBackButton
	{
		private readonly Page owner;
		private readonly LegalIdentity requestorIdentity;
		private readonly string requestorBareJid;
		private readonly string requestedIdentityId;
		private readonly string petitionId;
		private readonly string purpose;

		public PetitionIdentityPage(Page Owner, LegalIdentity RequestorIdentity, string RequestorBareJid,
			string RequestedIdentityId, string PetitionId, string Purpose)
		{
			this.owner = Owner;
			this.requestorIdentity = RequestorIdentity;
			this.requestorBareJid = RequestorBareJid;
			this.requestedIdentityId = RequestedIdentityId;
			this.petitionId = PetitionId;
			this.purpose = Purpose;
			this.BindingContext = this;
			InitializeComponent();

			this.LoadPhotos();
		}

		private async void LoadPhotos()
		{
			if (!(this.requestorIdentity.Attachments is null))
			{
				int i = this.TableView.Root.IndexOf(this.ButtonSection);
				TableSection PhotoSection = new TableSection();
				this.TableView.Root.Insert(i++, PhotoSection);

				foreach (Attachment Attachment in this.requestorIdentity.Attachments)
				{
					if (Attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
					{
						ViewCell ViewCell;

						try
						{
							KeyValuePair<string, TemporaryFile> P = await App.Contracts.GetAttachmentAsync(Attachment.Url, 10000);

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
		}

		private void AcceptButton_Clicked(object sender, EventArgs e)
		{
			App.Contracts.PetitionIdentityResponseAsync(this.requestedIdentityId, this.petitionId, this.requestorBareJid, true);
			App.ShowPage(this.owner, true);
		}

		private void DeclineButton_Clicked(object sender, EventArgs e)
		{
			App.Contracts.PetitionIdentityResponseAsync(this.requestedIdentityId, this.petitionId, this.requestorBareJid, false);
			App.ShowPage(this.owner, true);
		}

		private void IgnoreButton_Clicked(object sender, EventArgs e)
		{
			App.ShowPage(this.owner, true);
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
		public string Country => this.requestorIdentity["COUNTRY"];
		public bool IsApproved => this.requestorIdentity.State == IdentityState.Approved;
		public string Purpose => this.purpose;

		private static DateTime? CheckMin(DateTime? TP)
		{
			if (!TP.HasValue || TP.Value == DateTime.MinValue)
				return null;
			else
				return TP;
		}

		public bool BackClicked()
		{
			this.IgnoreButton_Clicked(this, new EventArgs());
			return true;
		}

	}
}
