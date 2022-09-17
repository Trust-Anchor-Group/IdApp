using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using IdApp.Pages.Contacts.Chat;
using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Main.XmppForm;
using IdApp.Pages.Things.MyThings;
using IdApp.Pages.Things.ReadSensor;
using IdApp.Services;
using IdApp.Services.Notification;
using IdApp.Services.Xmpp;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.Sensor;
using Waher.Persistence;
using Waher.Things;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Things.ViewThing
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public class ViewThingModel : XmppViewModel
	{
		private ContactInfo thing;

		/// <summary>
		/// Creates an instance of the <see cref="ViewThingModel"/> class.
		/// </summary>
		protected internal ViewThingModel()
			: base()
		{
			this.ClickCommand = new Command(async x => await this.Clicked(x));
			this.AddToListCommand = new Command(async _ => await this.AddToList(), _ => !this.InContacts);
			this.RemoveFromListCommand = new Command(async _ => await this.RemoveFromList(), _ => this.InContactsAndNotOwner);
			this.DeleteRulesCommand = new Command(async _ => await this.DeleteRules(), _ => this.IsConnected && this.IsOwner);
			this.DisownThingCommand = new Command(async _ => await this.DisownThing(), _ => this.IsConnected && this.IsOwner);
			this.ReadSensorCommand = new Command(async _ => await this.ReadSensor(), _ => this.IsConnected && this.IsSensor);
			this.ControlActuatorCommand = new Command(async _ => await this.ControlActuator(), _ => this.IsConnected && this.IsActuator);
			this.ChatCommand = new Command(async _ => await this.Chat(), _ => this.IsConnected && !this.IsNodeInConcentrator);

			this.Tags = new ObservableCollection<HumanReadableTag>();
			this.Notifications = new ObservableCollection<EventModel>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out ViewThingNavigationArgs args))
			{
				this.thing = args.Thing;

				if (this.thing.MetaData is not null)
				{
					this.Tags.Clear();

					foreach (Property Tag in this.thing.MetaData)
						this.Tags.Add(new HumanReadableTag(Tag));
				}

				if (args.Events is not null)
				{
					this.Notifications.Clear();

					int c = 0;

					foreach (NotificationEvent Event in args.Events)
					{
						this.Notifications.Add(new EventModel(Event.Received,
							await Event.GetCategoryIcon(this),
							await Event.GetDescription(this),
							Event,
							this));

						if (Event.Button == EventButton.Contacts)
							c++;
					}

					this.NrPendingChatMessages = c;
					this.HasPendingChatMessages = c > 0;
				}

				this.InContacts = !string.IsNullOrEmpty(this.thing.ObjectId);
				this.IsOwner = this.thing.Owner ?? false;
				this.IsSensor = this.thing.IsSensor ?? false;
				this.IsActuator = this.thing.IsActuator ?? false;
				this.IsConcentrator = this.thing.IsConcentrator ?? false;
				this.IsNodeInConcentrator = !string.IsNullOrEmpty(this.thing.NodeId) || !string.IsNullOrEmpty(this.thing.SourceId) || !string.IsNullOrEmpty(this.thing.Partition);
				this.SupportsSensorEvents = this.thing.SupportsSensorEvents ?? false;
				this.HasNotifications = this.Notifications.Count > 0;
			}

			this.AssignProperties();
			this.EvaluateAllCommands();

			this.XmppService.Xmpp.OnPresence += this.Xmpp_OnPresence;
			this.TagProfile.Changed += this.TagProfile_Changed;

			if (this.IsConnected && this.IsThingOnline)
			{
				if (!this.thing.IsSensor.HasValue ||
					!this.thing.IsActuator.HasValue ||
					!this.thing.IsConcentrator.HasValue ||
					!this.thing.SupportsSensorEvents.HasValue)
				{
					string FullJid = this.GetFullJid();

					if (!string.IsNullOrEmpty(FullJid))
					{
						this.XmppService.Xmpp.SendServiceDiscoveryRequest(FullJid, async (sender, e) =>
						{
							this.thing.IsSensor = e.HasFeature(SensorClient.NamespaceSensorData);
							this.thing.SupportsSensorEvents = e.HasFeature(SensorClient.NamespaceSensorEvents);
							this.thing.IsActuator = e.HasFeature(ControlClient.NamespaceControl);
							this.thing.IsConcentrator = e.HasFeature(ConcentratorServer.NamespaceConcentrator);

							if (this.InContacts)
								await Database.Update(this.thing);

							this.UiSerializer.BeginInvokeOnMainThread(() =>
							{
								this.IsSensor = this.thing.IsSensor ?? false;
								this.IsActuator = this.thing.IsActuator ?? false;
								this.IsConcentrator = this.thing.IsConcentrator ?? false;
								this.SupportsSensorEvents = this.thing.SupportsSensorEvents ?? false;
							});
						}, null);
					}
				}
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.XmppService.Xmpp.OnPresence -= this.Xmpp_OnPresence;
			this.TagProfile.Changed -= this.TagProfile_Changed;

			await base.OnDispose();
		}

		private Task Xmpp_OnPresence(object Sender, PresenceEventArgs e)
		{
			this.CalcThingIsOnline();
			return Task.CompletedTask;
		}

		private void CalcThingIsOnline()
		{
			if (this.thing is null)
				this.IsThingOnline = false;
			else
			{
				RosterItem Item = this.XmppService.Xmpp?.GetRosterItem(this.thing.BareJid);
				this.IsThingOnline = Item is not null && Item.HasLastPresence && Item.LastPresence.IsOnline;
			}
		}

		private string GetFullJid()
		{
			if (this.thing is null)
				return null;
			else
			{
				RosterItem Item = this.XmppService.Xmpp?.GetRosterItem(this.thing.BareJid);

				if (Item is null || !Item.HasLastPresence || !Item.LastPresence.IsOnline)
					return null;
				else
					return Item.LastPresenceFullJid;
			}
		}

		private void AssignProperties()
		{
			this.CalcThingIsOnline();
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.ClickCommand, this.AddToListCommand, this.RemoveFromListCommand, this.DeleteRulesCommand,
				this.DisownThingCommand, this.ReadSensorCommand, this.ControlActuatorCommand, this.ChatCommand);
		}

		/// <inheritdoc/>
		protected override void XmppService_ConnectionStateChanged(object Sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(e.State);
				this.EvaluateAllCommands();
			});
		}

		private void TagProfile_Changed(object Sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(this.AssignProperties);
		}

		#region Properties

		/// <summary>
		/// Holds a list of meta-data tags associated with a thing.
		/// </summary>
		public ObservableCollection<HumanReadableTag> Tags { get; }

		/// <summary>
		/// Holds a list of notifications.
		/// </summary>
		public ObservableCollection<EventModel> Notifications { get; }

		/// <summary>
		/// Command to bind to for detecting when a tag value has been clicked on.
		/// </summary>
		public System.Windows.Input.ICommand ClickCommand { get; }

		/// <summary>
		/// The command to bind to for adding a thing to the list
		/// </summary>
		public System.Windows.Input.ICommand AddToListCommand { get; }

		/// <summary>
		/// The command to bind to for removing a thing from the list
		/// </summary>
		public System.Windows.Input.ICommand RemoveFromListCommand { get; }

		/// <summary>
		/// The command to bind to for clearing rules for the thing.
		/// </summary>
		public System.Windows.Input.ICommand DeleteRulesCommand { get; }

		/// <summary>
		/// The command to bind to for disowning a thing
		/// </summary>
		public System.Windows.Input.ICommand DisownThingCommand { get; }

		/// <summary>
		/// The command to bind to for reading a sensor
		/// </summary>
		public System.Windows.Input.ICommand ReadSensorCommand { get; }

		/// <summary>
		/// The command to bind to for controlling an actuator
		/// </summary>
		public System.Windows.Input.ICommand ControlActuatorCommand { get; }

		/// <summary>
		/// The command to bind to for chatting with a thing
		/// </summary>
		public System.Windows.Input.ICommand ChatCommand { get; }

		/// <summary>
		/// See <see cref="InContacts"/>
		/// </summary>
		public static readonly BindableProperty InContactsProperty =
			BindableProperty.Create(nameof(InContacts), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact list.
		/// </summary>
		public bool InContacts
		{
			get => (bool)this.GetValue(InContactsProperty);
			set
			{
				this.SetValue(InContactsProperty, value);
				this.InContactsAndNotOwner = this.InContacts && !this.IsOwner;
			}
		}

		/// <summary>
		/// See <see cref="IsOwner"/>
		/// </summary>
		public static readonly BindableProperty IsOwnerProperty =
			BindableProperty.Create(nameof(IsOwner), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact.
		/// </summary>
		public bool IsOwner
		{
			get => (bool)this.GetValue(IsOwnerProperty);
			set
			{
				this.SetValue(IsOwnerProperty, value);
				this.InContactsAndNotOwner = this.InContacts && !this.IsOwner;
			}
		}

		/// <summary>
		/// See <see cref="InContactsAndNotOwner"/>
		/// </summary>
		public static readonly BindableProperty InContactsAndNotOwnerProperty =
			BindableProperty.Create(nameof(InContactsAndNotOwner), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact list.
		/// </summary>
		public bool InContactsAndNotOwner
		{
			get => (bool)this.GetValue(InContactsAndNotOwnerProperty);
			set => this.SetValue(InContactsAndNotOwnerProperty, value);
		}

		/// <summary>
		/// See <see cref="IsThingOnline"/>
		/// </summary>
		public static readonly BindableProperty IsThingOnlineProperty =
			BindableProperty.Create(nameof(IsThingOnline), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact.
		/// </summary>
		public bool IsThingOnline
		{
			get => (bool)this.GetValue(IsThingOnlineProperty);
			set => this.SetValue(IsThingOnlineProperty, value);
		}

		/// <summary>
		/// See <see cref="IsSensor"/>
		/// </summary>
		public static readonly BindableProperty IsSensorProperty =
			BindableProperty.Create(nameof(IsSensor), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a sensor
		/// </summary>
		public bool IsSensor
		{
			get => (bool)this.GetValue(IsSensorProperty);
			set => this.SetValue(IsSensorProperty, value);
		}

		/// <summary>
		/// See <see cref="IsActuator"/>
		/// </summary>
		public static readonly BindableProperty IsActuatorProperty =
			BindableProperty.Create(nameof(IsActuator), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is an actuator
		/// </summary>
		public bool IsActuator
		{
			get => (bool)this.GetValue(IsActuatorProperty);
			set => this.SetValue(IsActuatorProperty, value);
		}

		/// <summary>
		/// See <see cref="IsConcentrator"/>
		/// </summary>
		public static readonly BindableProperty IsConcentratorProperty =
			BindableProperty.Create(nameof(IsConcentrator), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a concentrator
		/// </summary>
		public bool IsConcentrator
		{
			get => (bool)this.GetValue(IsConcentratorProperty);
			set => this.SetValue(IsConcentratorProperty, value);
		}

		/// <summary>
		/// See <see cref="IsNodeInConcentrator"/>
		/// </summary>
		public static readonly BindableProperty IsNodeInConcentratorProperty =
			BindableProperty.Create(nameof(IsNodeInConcentrator), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a concentrator
		/// </summary>
		public bool IsNodeInConcentrator
		{
			get => (bool)this.GetValue(IsNodeInConcentratorProperty);
			set => this.SetValue(IsNodeInConcentratorProperty, value);
		}

		/// <summary>
		/// See <see cref="SupportsSensorEvents"/>
		/// </summary>
		public static readonly BindableProperty SupportsSensorEventsProperty =
			BindableProperty.Create(nameof(SupportsSensorEvents), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a sensor
		/// </summary>
		public bool SupportsSensorEvents
		{
			get => (bool)this.GetValue(SupportsSensorEventsProperty);
			set => this.SetValue(SupportsSensorEventsProperty, value);
		}

		/// <summary>
		/// See <see cref="HasNotifications"/>
		/// </summary>
		public static readonly BindableProperty HasNotificationsProperty =
			BindableProperty.Create(nameof(HasNotifications), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a sensor
		/// </summary>
		public bool HasNotifications
		{
			get => (bool)this.GetValue(HasNotificationsProperty);
			set => this.SetValue(HasNotificationsProperty, value);
		}

		/// <summary>
		/// See <see cref="HasPendingChatMessages"/>
		/// </summary>
		public static readonly BindableProperty HasPendingChatMessagesProperty =
			BindableProperty.Create(nameof(HasPendingChatMessages), typeof(bool), typeof(ViewThingModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is in the contact.
		/// </summary>
		public bool HasPendingChatMessages
		{
			get => (bool)this.GetValue(HasPendingChatMessagesProperty);
			set => this.SetValue(HasPendingChatMessagesProperty, value);
		}

		/// <summary>
		/// See <see cref="NrPendingChatMessages"/>
		/// </summary>
		public static readonly BindableProperty NrPendingChatMessagesProperty =
			BindableProperty.Create(nameof(NrPendingChatMessages), typeof(int), typeof(ViewThingModel), default(int));

		/// <summary>
		/// Gets or sets whether the identity is in the contact.
		/// </summary>
		public int NrPendingChatMessages
		{
			get => (int)this.GetValue(NrPendingChatMessagesProperty);
			set => this.SetValue(NrPendingChatMessagesProperty, value);
		}

		#endregion

		private Task Clicked(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return ViewClaimThing.ViewClaimThingViewModel.LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this.UiSerializer, this.LogService);
			else
				return Task.CompletedTask;
		}

		private async Task DeleteRules()
		{
			try
			{
				if (!await this.UiSerializer.DisplayAlert(
					LocalizationResourceManager.Current["Question"], LocalizationResourceManager.Current["DeleteRulesQuestion"],
					LocalizationResourceManager.Current["Yes"], LocalizationResourceManager.Current["Cancel"]))
				{
					return;
				}

				if (!await App.VerifyPin())
					return;

				TaskCompletionSource<bool> Result = new TaskCompletionSource<bool>();

				this.XmppService.IoT.ProvisioningClient.DeleteDeviceRules(this.thing.RegistryJid, this.thing.BareJid, this.thing.NodeId,
					this.thing.SourceId, this.thing.Partition, (sender, e) =>
				{
					if (e.Ok)
						Result.TrySetResult(true);
					else if (e.StanzaError is not null)
						Result.TrySetException(e.StanzaError);
					else
						Result.TrySetResult(false);

					return Task.CompletedTask;
				}, null);

				if (!await Result.Task)
					return;

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["RulesDeleted"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task DisownThing()
		{
			try
			{
				if (!await this.UiSerializer.DisplayAlert(
					LocalizationResourceManager.Current["Question"], LocalizationResourceManager.Current["DisownThingQuestion"],
					LocalizationResourceManager.Current["Yes"], LocalizationResourceManager.Current["Cancel"]))
				{
					return;
				}

				if (!await App.VerifyPin())
					return;

				(bool Succeeded, bool Done) = await this.NetworkService.TryRequest(() =>
					this.XmppService.ThingRegistry.Disown(this.thing.RegistryJid, this.thing.BareJid, this.thing.SourceId, this.thing.Partition, this.thing.NodeId));

				if (!Succeeded)
					return;

				if (this.InContacts)
				{
					await Database.Delete(this.thing);
					await Database.Provider.Flush();

					this.thing.ObjectId = null;
					this.InContacts = false;
				}

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["ThingDisowned"]);
				await this.NavigationService.GoBackAsync();
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task AddToList()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				if (!this.InContacts)
				{
					await Database.Insert(this.thing);
					this.InContacts = true;
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task RemoveFromList()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				if (this.InContacts)
				{
					await Database.Delete(this.thing);
					this.thing.ObjectId = null;
					this.InContacts = false;
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ReadSensor()
		{
			await this.NavigationService.GoToAsync(nameof(ReadSensorPage), new ViewThingNavigationArgs(this.thing,
				MyThingsViewModel.GetNotificationEvents(this, this.thing)));
		}

		private async Task ControlActuator()
		{
			try
			{
				string SelectedLanguage = Preferences.Get("user_selected_language", null);

				if (string.IsNullOrEmpty(this.thing.NodeId) && string.IsNullOrEmpty(this.thing.SourceId) && string.IsNullOrEmpty(this.thing.Partition))
					this.XmppService.IoT.ControlClient.GetForm(this.GetFullJid(), SelectedLanguage, this.ControlFormCallback, null);
				else
				{
					ThingReference ThingRef = new(this.thing.NodeId, this.thing.SourceId, this.thing.Partition);
					this.XmppService.IoT.ControlClient.GetForm(this.GetFullJid(), SelectedLanguage, this.ControlFormCallback, null, ThingRef);
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private Task ControlFormCallback(object Sender, DataFormEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				await this.NavigationService.GoToAsync(nameof(XmppFormPage), new XmppFormNavigationArgs(e.Form));
			});

			return Task.CompletedTask;
		}

		private async Task Chat()
		{
			try
			{
				string LegalId = this.thing?.LegalId;
				string FriendlyName = this.thing.FriendlyName;

				await this.NavigationService.GoToAsync(nameof(ChatPage),
					new ChatNavigationArgs(LegalId, this.thing.BareJid, FriendlyName)
					{
						UniqueId = this.thing.BareJid
					});
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

	}
}
