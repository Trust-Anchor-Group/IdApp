using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using EDaler;
using EDaler.Uris;
using IdApp.Pages.Contacts.Chat;
using IdApp.Pages.Identity.ViewIdentity;
using IdApp.Pages.Wallet;
using IdApp.Pages.Wallet.Payment;
using IdApp.Resx;
using IdApp.Services;
using IdApp.Services.Notification;
using IdApp.Services.UI.QR;
using Waher.Networking.XMPP;
using Waher.Persistence;
using Xamarin.Forms;

namespace IdApp.Pages.Contacts.MyContacts
{
	/// <summary>
	/// The view model to bind to when displaying the list of contacts.
	/// </summary>
	public class ContactListViewModel : BaseViewModel
	{
		private readonly Dictionary<CaseInsensitiveString, List<ContactInfoModel>> byBareJid;
		private TaskCompletionSource<ContactInfoModel> selection;

		/// <summary>
		/// Creates an instance of the <see cref="ContactListViewModel"/> class.
		/// </summary>
		protected internal ContactListViewModel()
		{
			this.ScanQrCodeCommand = new Command(async _ => await this.ScanQrCode());

			this.Contacts = new ObservableCollection<ContactInfoModel>();
			this.byBareJid = new();

			this.Description = AppResources.ContactsDescription;
			this.Action = SelectContactAction.ViewIdentity;
			this.selection = null;
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out ContactListNavigationArgs args))
			{
				this.Description = args.Description;
				this.Action = args.Action;
				this.selection = args.Selection;
				this.CanScanQrCode = args.CanScanQrCode;
			}



			SortedDictionary<CaseInsensitiveString, ContactInfo> Sorted = new();
			Dictionary<CaseInsensitiveString, bool> Jids = new();

			foreach (ContactInfo Info in await Database.Find<ContactInfo>())
			{
				Jids[Info.BareJid] = true;

				if (Info.IsThing.HasValue && Info.IsThing.Value)        // Include those with IsThing=null
					continue;

				if (Info.AllowSubscriptionFrom.HasValue && !Info.AllowSubscriptionFrom.Value)
					continue;

				Add(Sorted, Info.FriendlyName, Info);
			}

			foreach (RosterItem Item in this.XmppService.Xmpp.Roster)
			{
				if (Jids.ContainsKey(Item.BareJid))
					continue;

				ContactInfo Info = new()
				{
					BareJid = Item.BareJid,
					FriendlyName = Item.NameOrBareJid,
					IsThing = null
				};

				await Database.Insert(Info);

				Add(Sorted, Info.FriendlyName, Info);
			}

			NotificationEvent[] Events;

			this.Contacts.Clear();

			foreach (CaseInsensitiveString Category in this.NotificationService.GetCategories(EventButton.Contacts))
			{
				if (this.NotificationService.TryGetNotificationEvents(EventButton.Contacts, Category, out Events))
				{
					if (Sorted.TryGetValue(Category, out ContactInfo Info))
						Sorted.Remove(Category);
					else
					{
						Info = await ContactInfo.FindByBareJid(Category);

						if (Info is not null)
							Remove(Sorted, Info.FriendlyName, Info);
						else
						{
							Info = new()
							{
								BareJid = Category,
								FriendlyName = Category,
								IsThing = null
							};
						}
					}

					this.Contacts.Add(new ContactInfoModel(this, Info, Events));
				}
			}

			foreach (ContactInfo Info in Sorted.Values)
			{
				if (!this.NotificationService.TryGetNotificationEvents(EventButton.Contacts, Info.BareJid, out Events))
					Events = new NotificationEvent[0];

				this.Contacts.Add(new ContactInfoModel(this, Info, Events));
			}

			this.byBareJid.Clear();

			foreach (ContactInfoModel Contact in this.Contacts)
			{
				if (string.IsNullOrEmpty(Contact.BareJid))
					continue;

				if (!this.byBareJid.TryGetValue(Contact.BareJid, out List<ContactInfoModel> Contacts))
				{
					Contacts = new List<ContactInfoModel>();
					this.byBareJid[Contact.BareJid] = Contacts;
				}

				Contacts.Add(Contact);
			}

			this.ShowContactsMissing = Sorted.Count == 0;

			this.XmppService.Xmpp.OnPresence += this.Xmpp_OnPresence;
			this.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
			this.NotificationService.OnNotificationsDeleted += this.NotificationService_OnNotificationsDeleted;
		}

		private static void Add(SortedDictionary<CaseInsensitiveString, ContactInfo> Sorted, CaseInsensitiveString Name, ContactInfo Info)
		{
			if (Sorted.ContainsKey(Name))
			{
				int i = 1;
				string Suffix;

				do
				{
					Suffix = " " + (++i).ToString();
				}
				while (Sorted.ContainsKey(Name + Suffix));

				Sorted[Name + Suffix] = Info;
			}
			else
				Sorted[Name] = Info;
		}

		private static void Remove(SortedDictionary<CaseInsensitiveString, ContactInfo> Sorted, CaseInsensitiveString Name, ContactInfo Info)
		{
			int i = 1;
			string Suffix = string.Empty;

			while (Sorted.TryGetValue(Name + Suffix, out ContactInfo Info2))
			{
				if (Info2.BareJid == Info.BareJid &&
					Info2.SourceId == Info.SourceId &&
					Info2.Partition == Info.Partition &&
					Info2.NodeId == Info.NodeId &&
					Info2.LegalId == Info.LegalId)
				{
					Sorted.Remove(Name + Suffix);

					i++;
					string Suffix2 = " " + i.ToString();

					while (Sorted.TryGetValue(Name + Suffix2, out Info2))
					{
						Sorted[Name + Suffix] = Info2;
						Sorted.Remove(Name + Suffix2);

						i++;
						Suffix2 = " " + i.ToString();
					}

					return;
				}

				i++;
				Suffix = " " + i.ToString();
			}
		}

		/// <inheritdoc/>
		protected override Task OnDispose()
		{
			this.XmppService.Xmpp.OnPresence -= this.Xmpp_OnPresence;
			this.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;
			this.NotificationService.OnNotificationsDeleted -= this.NotificationService_OnNotificationsDeleted;

			if (this.Action != SelectContactAction.Select)
			{
				this.ShowContactsMissing = false;
				this.Contacts.Clear();
			}

			this.selection?.TrySetResult(null);

			return base.OnDispose();
		}

		/// <summary>
		/// See <see cref="ShowContactsMissing"/>
		/// </summary>
		public static readonly BindableProperty ShowContactsMissingProperty =
			BindableProperty.Create(nameof(ShowContactsMissing), typeof(bool), typeof(ContactListViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether to show a contacts missing alert or not.
		/// </summary>
		public bool ShowContactsMissing
		{
			get => (bool)this.GetValue(ShowContactsMissingProperty);
			set => this.SetValue(ShowContactsMissingProperty, value);
		}

		/// <summary>
		/// <see cref="Description"/>
		/// </summary>
		public static readonly BindableProperty DescriptionProperty =
			BindableProperty.Create(nameof(Description), typeof(string), typeof(ContactListViewModel), default(string));

		/// <summary>
		/// The description to present to the user.
		/// </summary>
		public string Description
		{
			get => (string)this.GetValue(DescriptionProperty);
			set => this.SetValue(DescriptionProperty, value);
		}

		/// <summary>
		/// <see cref="CanScanQrCode"/>
		/// </summary>
		public static readonly BindableProperty CanScanQrCodeProperty =
			BindableProperty.Create(nameof(CanScanQrCode), typeof(bool), typeof(ContactListViewModel), default(bool));

		/// <summary>
		/// The description to present to the user.
		/// </summary>
		public bool CanScanQrCode
		{
			get => (bool)this.GetValue(CanScanQrCodeProperty);
			set => this.SetValue(CanScanQrCodeProperty, value);
		}

		/// <summary>
		/// <see cref="Action"/>
		/// </summary>
		public static readonly BindableProperty ActionProperty =
			BindableProperty.Create(nameof(Action), typeof(SelectContactAction), typeof(ContactListViewModel), default(SelectContactAction));

		/// <summary>
		/// The action to take when contact has been selected.
		/// </summary>
		public SelectContactAction Action
		{
			get => (SelectContactAction)this.GetValue(ActionProperty);
			set => this.SetValue(ActionProperty, value);
		}

		/// <summary>
		/// Holds the list of contacts to display.
		/// </summary>
		public ObservableCollection<ContactInfoModel> Contacts { get; }

		/// <summary>
		/// See <see cref="SelectedContact"/>
		/// </summary>
		public static readonly BindableProperty SelectedContactProperty =
			BindableProperty.Create(nameof(SelectedContact), typeof(ContactInfoModel), typeof(ContactListViewModel), default(ContactInfoModel),
				propertyChanged: (b, oldValue, newValue) =>
				{
					if (b is ContactListViewModel viewModel && newValue is ContactInfoModel Contact)
						viewModel.OnSelected(Contact);
				});

		private void OnSelected(ContactInfoModel Contact)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				switch (this.Action)
				{
					case SelectContactAction.MakePayment:
						StringBuilder sb = new();

						sb.Append("edaler:");

						if (!string.IsNullOrEmpty(Contact.LegalId))
						{
							sb.Append("ti=");
							sb.Append(Contact.LegalId);
						}
						else if (!string.IsNullOrEmpty(Contact.BareJid))
						{
							sb.Append("t=");
							sb.Append(Contact.BareJid);
						}
						else
							break;

						Balance Balance = await this.XmppService.Wallet.GetBalanceAsync();

						sb.Append(";cu=");
						sb.Append(Balance.Currency);

						if (!EDalerUri.TryParse(sb.ToString(), out EDalerUri Parsed))
							break;

						await this.NavigationService.GoToAsync(nameof(PaymentPage), new EDalerUriNavigationArgs(Parsed)
						{
							ReturnRoute = "../.."
						});
						break;

					case SelectContactAction.ViewIdentity:
					default:
						if (Contact.LegalIdentity is not null)
						{
							await this.NavigationService.GoToAsync(nameof(ViewIdentityPage),
								new ViewIdentityNavigationArgs(Contact.LegalIdentity));
						}
						else if (!string.IsNullOrEmpty(Contact.LegalId))
							await this.ContractOrchestratorService.OpenLegalIdentity(Contact.LegalId, AppResources.ScannedQrCode);
						else if (!string.IsNullOrEmpty(Contact.BareJid))
						{
							await this.NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(Contact.Contact)
							{
								UniqueId = Contact.BareJid
							});
						}

						break;

					case SelectContactAction.Select:
						this.selection?.TrySetResult(Contact);
						await this.NavigationService.GoBackAsync();
						break;
				}
			});
		}

		/// <summary>
		/// The currently selected contact, if any.
		/// </summary>
		public ContactInfoModel SelectedContact
		{
			get => (ContactInfoModel)this.GetValue(SelectedContactProperty);
			set => this.SetValue(SelectedContactProperty, value);
		}

		/// <summary>
		/// Command executed when the user wants to scan a contact from a QR Code.
		/// </summary>
		public ICommand ScanQrCodeCommand { get; }

		private async Task ScanQrCode()
		{
			await QrCode.ScanQrCode(AppResources.ScanQRCode, async code =>
			{
				if (Constants.UriSchemes.StartsWithIdScheme(code))
				{
					this.OnSelected(new ContactInfoModel(this, new ContactInfo()
					{
						LegalId = Constants.UriSchemes.RemoveScheme(code)
					}, new NotificationEvent[0]));
				}
				else if (!string.IsNullOrEmpty(code))
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.TheSpecifiedCodeIsNotALegalIdentity);
			});
		}

		private Task Xmpp_OnPresence(object Sender, PresenceEventArgs e)
		{
			if (this.byBareJid.TryGetValue(e.FromBareJID, out List<ContactInfoModel> Contacts))
			{
				foreach (ContactInfoModel Contact in Contacts)
					Contact.PresenceUpdated();
			}

			return Task.CompletedTask;
		}

		private void NotificationService_OnNewNotification(object Sender, NotificationEventArgs e)
		{
			if (e.Event.Button == EventButton.Contacts)
				this.UpdateNotifications(e.Event.Category);
		}

		private void UpdateNotifications()
		{
			foreach (CaseInsensitiveString Category in this.NotificationService.GetCategories(EventButton.Contacts))
				this.UpdateNotifications(Category);
		}

		private void UpdateNotifications(CaseInsensitiveString Category)
		{
			if (this.byBareJid.TryGetValue(Category, out List<ContactInfoModel> Contacts) &&
				this.NotificationService.TryGetNotificationEvents(EventButton.Contacts, Category, out NotificationEvent[] Events))
			{
				foreach (ContactInfoModel Contact in Contacts)
					Contact.NotificationsUpdated(Events);
			}
		}

		private void NotificationService_OnNotificationsDeleted(object Sender, NotificationEventsArgs e)
		{
			this.UpdateNotifications();
		}

	}
}
