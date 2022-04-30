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
using IdApp.Services.UI.QR;
using Waher.Networking.XMPP;
using Waher.Persistence;
using Xamarin.Forms;

namespace IdApp.Pages.Contacts
{
	/// <summary>
	/// The view model to bind to when displaying the list of contacts.
	/// </summary>
	public class ContactListViewModel : BaseViewModel
	{
		private TaskCompletionSource<ContactInfo> selection;

		/// <summary>
		/// Creates an instance of the <see cref="ContactListViewModel"/> class.
		/// </summary>
		protected internal ContactListViewModel()
		{
			this.ScanQrCodeCommand = new Command(async _ => await this.ScanQrCode());

			this.Contacts = new ObservableCollection<ContactInfo>();

			this.Description = AppResources.ContactsDescription;
			this.Action = SelectContactAction.ViewIdentity;
			this.selection = null;
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out ContactListNavigationArgs args))
			{
				this.Description = args.Description;
				this.Action = args.Action;
				this.selection = args.Selection;
				this.CanScanQrCode = args.CanScanQrCode;
			}

			SortedDictionary<string, ContactInfo> Sorted = new();
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

			this.Contacts.Clear();
			foreach (ContactInfo Info in Sorted.Values)
				this.Contacts.Add(Info);

			this.ShowContactsMissing = Sorted.Count == 0;
		}

		private static void Add(SortedDictionary<string, ContactInfo> Sorted, string Name, ContactInfo Info)
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

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			if (this.Action != SelectContactAction.Select)
			{
				this.ShowContactsMissing = false;
				this.Contacts.Clear();
			}

			this.selection?.TrySetResult(null);

			await base.DoUnbind();
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
		public ObservableCollection<ContactInfo> Contacts { get; }

		/// <summary>
		/// See <see cref="SelectedContact"/>
		/// </summary>
		public static readonly BindableProperty SelectedContactProperty =
			BindableProperty.Create(nameof(SelectedContact), typeof(ContactInfo), typeof(ContactListViewModel), default(ContactInfo),
				propertyChanged: (b, oldValue, newValue) =>
				{
					if (b is ContactListViewModel viewModel && newValue is ContactInfo Contact)
						viewModel.OnSelected(Contact);
				});

		private void OnSelected(ContactInfo Contact)
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
						if (!(Contact.LegalIdentity is null))
						{
							await this.NavigationService.GoToAsync(nameof(ViewIdentityPage),
								new ViewIdentityNavigationArgs(Contact.LegalIdentity, null));
						}
						else if (!string.IsNullOrEmpty(Contact.LegalId))
							await this.ContractOrchestratorService.OpenLegalIdentity(Contact.LegalId, AppResources.ScannedQrCode);
						else if (!string.IsNullOrEmpty(Contact.BareJid))
							await this.NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(Contact));

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
		public ContactInfo SelectedContact
		{
			get => (ContactInfo)this.GetValue(SelectedContactProperty);
			set => this.SetValue(SelectedContactProperty, value);
		}

		/// <summary>
		/// Command executed when the user wants to scan a contact from a QR Code.
		/// </summary>
		public ICommand ScanQrCodeCommand { get; }

		private async Task ScanQrCode()
		{
			await QrCode.ScanQrCode(this.NavigationService, AppResources.ScanQRCode, async code =>
			{
				if (Constants.UriSchemes.StartsWithIdScheme(code))
					this.OnSelected(new ContactInfo() { LegalId = Constants.UriSchemes.RemoveScheme(code) });
				else
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.TheSpecifiedCodeIsNotALegalIdentity);
			});
		}
	}
}