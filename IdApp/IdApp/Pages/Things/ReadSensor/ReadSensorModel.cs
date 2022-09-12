using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Pages.Things.ReadSensor.Model;
using IdApp.Pages.Things.ViewClaimThing;
using IdApp.Pages.Things.ViewThing;
using IdApp.Services;
using IdApp.Services.Xmpp;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Sensor;
using Waher.Things;
using Waher.Things.SensorData;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Things.ReadSensor
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public class ReadSensorModel : XmppViewModel
	{
		private ContactInfo thing;
		private ThingReference thingRef;
		private SensorDataClientRequest request;

		/// <summary>
		/// Creates an instance of the <see cref="ReadSensorModel"/> class.
		/// </summary>
		protected internal ReadSensorModel()
			: base()
		{
			this.ClickCommand = new Command(async x => await this.LabelClicked(x));

			this.SensorData = new ObservableCollection<object>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out ViewThingNavigationArgs args))
			{
				this.thing = args.Thing;

				if (string.IsNullOrEmpty(this.thing.NodeId) && string.IsNullOrEmpty(this.thing.SourceId) && string.IsNullOrEmpty(this.thing.Partition))
					this.thingRef = null;
				else
					this.thingRef = new ThingReference(this.thing.NodeId, this.thing.SourceId, this.thing.Partition);

				if (this.thing.MetaData is not null && this.thing.MetaData.Length > 0)
				{
					this.SensorData.Add(new HeaderModel(LocalizationResourceManager.Current["GeneralInformation"]));

					foreach (Property Tag in this.thing.MetaData)
						this.SensorData.Add(new HumanReadableTag(Tag));
				}

				this.SupportsSensorEvents = this.thing.SupportsSensorEvents ?? false;
			}

			this.AssignProperties();
			this.EvaluateAllCommands();

			this.XmppService.Xmpp.OnPresence += this.Xmpp_OnPresence;
			this.TagProfile.Changed += this.TagProfile_Changed;

			if (this.thingRef is null)
				this.request = this.XmppService.IoT.SensorClient.RequestReadout(this.GetFullJid(), FieldType.All);
			else
				this.request = this.XmppService.IoT.SensorClient.RequestReadout(this.GetFullJid(), new ThingReference[] { this.thingRef }, FieldType.All);

			this.request.OnStateChanged += this.Request_OnStateChanged;
			this.request.OnFieldsReceived += this.Request_OnFieldsReceived;
			this.request.OnErrorsReceived += this.Request_OnErrorsReceived;

		}

		private Task Request_OnStateChanged(object Sender, SensorDataReadoutState NewState)
		{
			this.Status = NewState switch
			{
				SensorDataReadoutState.Requested => LocalizationResourceManager.Current["SensorDataRequested"],
				SensorDataReadoutState.Accepted => LocalizationResourceManager.Current["SensorDataAccepted"],
				SensorDataReadoutState.Cancelled => LocalizationResourceManager.Current["SensorDataCancelled"],
				SensorDataReadoutState.Done => LocalizationResourceManager.Current["SensorDataDone"],
				SensorDataReadoutState.Failure => LocalizationResourceManager.Current["SensorDataFailure"],
				SensorDataReadoutState.Receiving => LocalizationResourceManager.Current["SensorDataReceiving"],
				SensorDataReadoutState.Started => LocalizationResourceManager.Current["SensorDataStarted"],
				_ => string.Empty,
			};
			return Task.CompletedTask;
		}

		private Task Request_OnFieldsReceived(object Sender, System.Collections.Generic.IEnumerable<Field> NewFields)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				// TODO
			});

			return Task.CompletedTask;
		}

		private Task Request_OnErrorsReceived(object Sender, System.Collections.Generic.IEnumerable<ThingError> NewErrors)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				// TODO
			});

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.XmppService.Xmpp.OnPresence -= this.Xmpp_OnPresence;
			this.TagProfile.Changed -= this.TagProfile_Changed;

			if (this.request is not null &&
				(this.request.State == SensorDataReadoutState.Receiving ||
				this.request.State == SensorDataReadoutState.Accepted ||
				this.request.State == SensorDataReadoutState.Requested ||
				this.request.State == SensorDataReadoutState.Started))
			{
				await this.request.Cancel();
			}

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
				RosterItem Item = this.XmppService.Xmpp[this.thing.BareJid];
				this.IsThingOnline = Item is not null && Item.HasLastPresence && Item.LastPresence.IsOnline;
			}
		}

		private string GetFullJid()
		{
			if (this.thing is null)
				return null;
			else
			{
				RosterItem Item = this.XmppService.Xmpp[this.thing.BareJid];

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
		public ObservableCollection<object> SensorData { get; }

		/// <summary>
		/// See <see cref="IsThingOnline"/>
		/// </summary>
		public static readonly BindableProperty IsThingOnlineProperty =
			BindableProperty.Create(nameof(IsThingOnline), typeof(bool), typeof(ReadSensorModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact.
		/// </summary>
		public bool IsThingOnline
		{
			get => (bool)this.GetValue(IsThingOnlineProperty);
			set => this.SetValue(IsThingOnlineProperty, value);
		}

		/// <summary>
		/// See <see cref="SupportsSensorEvents"/>
		/// </summary>
		public static readonly BindableProperty SupportsSensorEventsProperty =
			BindableProperty.Create(nameof(SupportsSensorEvents), typeof(bool), typeof(ReadSensorModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is a sensor
		/// </summary>
		public bool SupportsSensorEvents
		{
			get => (bool)this.GetValue(SupportsSensorEventsProperty);
			set => this.SetValue(SupportsSensorEventsProperty, value);
		}

		/// <summary>
		/// See <see cref="HasStatus"/>
		/// </summary>
		public static readonly BindableProperty HasStatusProperty =
			BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(ReadSensorModel), default(bool));

		/// <summary>
		/// Gets or sets whether there's a status message to display
		/// </summary>
		public bool HasStatus
		{
			get => (bool)this.GetValue(HasStatusProperty);
			set => this.SetValue(HasStatusProperty, value);
		}

		/// <summary>
		/// See <see cref="Status"/>
		/// </summary>
		public static readonly BindableProperty StatusProperty =
			BindableProperty.Create(nameof(Status), typeof(string), typeof(ReadSensorModel), default(string));

		/// <summary>
		/// Gets or sets a status message.
		/// </summary>
		public string Status
		{
			get => (string)this.GetValue(StatusProperty);
			set
			{
				this.SetValue(StatusProperty, value);
				this.HasStatus = !string.IsNullOrEmpty(value);
			}
		}

		/// <summary>
		/// Command to bind to for detecting when a tag value has been clicked on.
		/// </summary>
		public System.Windows.Input.ICommand ClickCommand { get; }

		#endregion

		private Task LabelClicked(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return ViewClaimThingViewModel.LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this.UiSerializer, this.LogService);
			else
				return Task.CompletedTask;
		}

	}
}
