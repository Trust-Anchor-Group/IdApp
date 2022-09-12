using IdApp.Popups.Xmpp.RemoveSubscription;
using IdApp.Popups.Xmpp.SubscribeTo;
using IdApp.Services;
using IdApp.Services.Notification;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Contacts.MyContacts
{
	/// <summary>
	/// Contact Information model, including related notification information.
	/// </summary>
	public class ContactInfoModel : INotifyPropertyChanged
	{
		private readonly ServiceReferences references;
		private readonly ContactInfo contact;
		private NotificationEvent[] events;

		private readonly ICommand toggleSubscriptionCommand;

		/// <summary>
		/// Contact Information model, including related notification information.
		/// </summary>
		/// <param name="References">Service references.</param>
		/// <param name="Contact">Contact information.</param>
		/// <param name="Events">Notification events</param>
		public ContactInfoModel(ServiceReferences References, ContactInfo Contact, params NotificationEvent[] Events)
		{
			this.references = References;
			this.contact = Contact;
			this.events = Events;

			this.toggleSubscriptionCommand = new Command(async () => await this.ToggleSubscription(), () => this.CanToggleSubscription());
		}

		/// <summary>
		/// Contact Information object in database.
		/// </summary>
		public ContactInfo Contact => this.contact;

		/// <summary>
		/// Bare JID of contact.
		/// </summary>
		public CaseInsensitiveString BareJid => this.contact.BareJid;

		/// <summary>
		/// Legal ID of contact.
		/// </summary>
		public CaseInsensitiveString LegalId => this.contact.LegalId;

		/// <summary>
		/// Legal Identity object.
		/// </summary>
		public LegalIdentity LegalIdentity => this.contact.LegalIdentity;

		/// <summary>
		/// Friendly name.
		/// </summary>
		public string FriendlyName => this.contact.FriendlyName;

		/// <summary>
		/// Source ID
		/// </summary>
		public string SourceId => this.contact.SourceId;

		/// <summary>
		/// Partition
		/// </summary>
		public string Partition => this.contact.Partition;

		/// <summary>
		/// Node ID
		/// </summary>
		public string NodeId => this.contact.NodeId;

		/// <summary>
		/// Registry JID
		/// </summary>
		public CaseInsensitiveString RegistryJid => this.contact.RegistryJid;

		/// <summary>
		/// Subscribe to this contact
		/// </summary>
		public bool? SubcribeTo => this.contact.SubcribeTo;

		/// <summary>
		/// Allow subscriptions from this contact
		/// </summary>
		public bool? AllowSubscriptionFrom => this.contact.AllowSubscriptionFrom;

		/// <summary>
		/// The contact is a thing
		/// </summary>
		public bool? IsThing => this.contact.IsThing;

		/// <summary>
		/// If the account is registered as the owner of the thing.
		/// </summary>
		public bool? Owner => this.contact.Owner;

		/// <summary>
		/// Meta-data related to a thing.
		/// </summary>
		public Property[] MetaData => this.contact.MetaData;

		/// <summary>
		/// Notification events.
		/// </summary>
		public NotificationEvent[] Events => this.events;

		/// <summary>
		/// If the contact has associated events.
		/// </summary>
		public bool HasEvents => this.events is not null && this.events.Length > 0;

		/// <summary>
		/// Number of events associated with contact.
		/// </summary>
		public int NrEvents => this.events?.Length ?? 0;

		/// <summary>
		/// A color representing the current connection state of the contact.
		/// </summary>
		public Color ConnectionColor
		{
			get
			{
				if (string.IsNullOrEmpty(this.contact.BareJid))
					return Color.Transparent;

				RosterItem Item = this.references.XmppService.Xmpp?.GetRosterItem(this.contact.BareJid);
				if (Item is null)
					return Color.Transparent;

				if (Item.State != SubscriptionState.To && Item.State != SubscriptionState.Both)
					return Color.Transparent;

				if (!Item.HasLastPresence)
					return Color.LightSalmon;

				return Item.LastPresence.Availability switch
				{
					Availability.Online or Availability.Chat => Color.LightGreen,
					Availability.Away or Availability.ExtendedAway => Color.LightYellow,
					_ => Color.LightSalmon,
				};
			}
		}

		/// <summary>
		/// Command to execute when user wants to toggle XMPP subcription.
		/// </summary>
		public ICommand ToggleSubscriptionCommand => this.toggleSubscriptionCommand;

		/// <summary>
		/// If toggle subscription can be performed on the contact.
		/// </summary>
		/// <returns>If command is enabled.</returns>
		public bool CanToggleSubscription()
		{
			return !string.IsNullOrEmpty(this.contact.BareJid);
		}

		/// <summary>
		/// Subscribes to an unsubscribed contact; unsubscribes from a subscribed one, with user permission.
		/// </summary>
		/// <returns></returns>
		public async Task ToggleSubscription()
		{
			if (string.IsNullOrEmpty(this.contact.BareJid))
				return;

			RosterItem Item = this.references.XmppService.Xmpp?.GetRosterItem(this.contact.BareJid);
			bool Subscribed;

			if (Item is null)
				Subscribed = false;
			else
				Subscribed = Item.State == SubscriptionState.To || Item.State == SubscriptionState.Both;

			if (Subscribed)
			{
				if (!await this.references.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Question"],
					string.Format(LocalizationResourceManager.Current["RemoveSubscriptionFrom"], this.FriendlyName),
					LocalizationResourceManager.Current["Yes"], LocalizationResourceManager.Current["Cancel"]))
				{
					return;
				}

				this.references.XmppService.Xmpp.RequestPresenceUnsubscription(this.BareJid);

				if (Item.State == SubscriptionState.From || Item.State == SubscriptionState.Both)
				{
					RemoveSubscriptionPopupPage Page = new(this.BareJid);

					await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(Page);
					bool? Remove = await Page.Result;

					if (Remove.HasValue && Remove.Value)
					{
						this.references.XmppService.Xmpp.RequestRevokePresenceSubscription(this.BareJid);

						if (this.contact.AllowSubscriptionFrom.HasValue && this.contact.AllowSubscriptionFrom.Value)
						{
							this.contact.AllowSubscriptionFrom = null;
							await Database.Update(this.contact);
						}
					}
				}
			}
			else
			{
				SubscribeToPopupPage SubscribeToPopupPage = new(this.BareJid);

				await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(SubscribeToPopupPage);
				bool? SubscribeTo = await SubscribeToPopupPage.Result;

				if (SubscribeTo.HasValue && SubscribeTo.Value)
				{
					string IdXml;

					if (this.references.TagProfile.LegalIdentity is null)
						IdXml = string.Empty;
					else
					{
						StringBuilder Xml = new();
						this.references.TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
						IdXml = Xml.ToString();
					}

					this.references.XmppService.Xmpp.RequestPresenceSubscription(this.BareJid, IdXml);
				}
			}
		}

		/// <summary>
		/// Method called when presence for contact has been updated.
		/// </summary>
		public void PresenceUpdated()
		{
			this.OnPropertyChanged(nameof(this.ConnectionColor));
		}

		/// <summary>
		/// Called when a property has changed.
		/// </summary>
		/// <param name="PropertyName">Name of property</param>
		public void	OnPropertyChanged(string PropertyName)
		{
			try
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
			}
			catch (Exception ex)
			{
				this.references.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Method called when notifications for the item have been updated.
		/// </summary>
		/// <param name="Events">Updated set of events.</param>
		public void NotificationsUpdated(NotificationEvent[] Events)
		{
			this.events = Events;

			this.OnPropertyChanged(nameof(this.Events));
			this.OnPropertyChanged(nameof(this.HasEvents));
			this.OnPropertyChanged(nameof(this.NrEvents));
		}

	}
}
