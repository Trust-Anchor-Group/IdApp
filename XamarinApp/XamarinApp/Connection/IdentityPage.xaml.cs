using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Xamarin.Forms;
using Waher.Content;
using Waher.IoTGateway.Setup;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace XamarinApp.Connection
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(true)]
	public partial class IdentityPage : ContentPage, INotifyPropertyChanged, ILegalIdentityChanged
	{
		private readonly XmppConfiguration xmppConfiguration;

		public IdentityPage(XmppConfiguration XmppConfiguration)
		{
			this.xmppConfiguration = XmppConfiguration;
			this.BindingContext = this;
			InitializeComponent();

			this.LoadPhotos();
		}

		private async void LoadPhotos()
		{
			if (!(this.xmppConfiguration.LegalIdentity.Attachments is null))
			{
				int i = this.TableView.Root.IndexOf(this.ButtonSection);
				TableSection PhotoSection = new TableSection();
				this.TableView.Root.Insert(i++, PhotoSection);

				foreach (Attachment Attachment in this.xmppConfiguration.LegalIdentity.Attachments)
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

		private async void BackButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				if (this.xmppConfiguration.Step > 0)
				{
					this.xmppConfiguration.Step--;
					await Database.Update(this.xmppConfiguration);
				}

				await App.ShowPage();
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		public DateTime Created => this.xmppConfiguration.LegalIdentity.Created;
		public DateTime? Updated => CheckMin(this.xmppConfiguration.LegalIdentity.Updated);
		public string LegalId => this.xmppConfiguration.LegalIdentity.Id;
		public string BareJid => App.Xmpp?.BareJID ?? string.Empty;
		public string State => this.xmppConfiguration.LegalIdentity.State.ToString();
		public DateTime? From => CheckMin(this.xmppConfiguration.LegalIdentity.From);
		public DateTime? To => CheckMin(this.xmppConfiguration.LegalIdentity.To);
		public string FirstName => this.xmppConfiguration.LegalIdentity["FIRST"];
		public string MiddleNames => this.xmppConfiguration.LegalIdentity["MIDDLE"];
		public string LastNames => this.xmppConfiguration.LegalIdentity["LAST"];
		public string PNr => this.xmppConfiguration.LegalIdentity["PNR"];
		public string Address => this.xmppConfiguration.LegalIdentity["ADDR"];
		public string Address2 => this.xmppConfiguration.LegalIdentity["ADDR2"];
		public string PostalCode => this.xmppConfiguration.LegalIdentity["ZIP"];
		public string Area => this.xmppConfiguration.LegalIdentity["AREA"];
		public string City => this.xmppConfiguration.LegalIdentity["CITY"];
		public string Region => this.xmppConfiguration.LegalIdentity["REGION"];
		public string Country => this.xmppConfiguration.LegalIdentity["COUNTRY"];
		public bool IsApproved => this.xmppConfiguration.LegalIdentity.State == IdentityState.Approved;

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
				this.xmppConfiguration.Step++;
				await Database.Update(this.xmppConfiguration);

				await App.ShowPage();
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
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

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, new EventArgs());
			return true;
		}
	}
}
