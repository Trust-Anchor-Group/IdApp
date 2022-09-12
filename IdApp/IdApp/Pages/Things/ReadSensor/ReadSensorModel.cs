﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
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

		private string GetFieldTypeString(FieldType Type)
		{
			if (Type.HasFlag(FieldType.Identity))
				return LocalizationResourceManager.Current["SensorDataHeaderIdentity"];
			else if (Type.HasFlag(FieldType.Status))
				return LocalizationResourceManager.Current["SensorDataHeaderStatus"];
			else if (Type.HasFlag(FieldType.Momentary))
				return LocalizationResourceManager.Current["SensorDataHeaderMomentary"];
			else if (Type.HasFlag(FieldType.Peak))
				return LocalizationResourceManager.Current["SensorDataHeaderPeak"];
			else if (Type.HasFlag(FieldType.Computed))
				return LocalizationResourceManager.Current["SensorDataHeaderComputed"];
			else if (Type.HasFlag(FieldType.Historical))
				return LocalizationResourceManager.Current["SensorDataHeaderHistorical"];
			else
				return LocalizationResourceManager.Current["SensorDataHeaderOther"];
		}

		private Task Request_OnFieldsReceived(object Sender, IEnumerable<Field> NewFields)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				string Category;
				HeaderModel CategoryHeader = null;
				string s;
				int CategoryIndex = 0;
				int i, j, c;

				foreach (Field Field in NewFields)
				{
					if (Field.Type.HasFlag(FieldType.Historical))
					{
						// TODO
					}
					else
					{
						Category = this.GetFieldTypeString(Field.Type);
						s = Field.Name;

						if (CategoryHeader is null || CategoryHeader.Label != Category)
						{
							CategoryHeader = null;
							CategoryIndex = 0;

							foreach (object Item in this.SensorData)
							{
								if (Item is HeaderModel Header && Header.Label == Category)
								{
									CategoryHeader = Header;
									break;
								}
								else
									CategoryIndex++;
							}

							if (CategoryHeader is null)
							{
								CategoryHeader = new HeaderModel(Category);
								this.SensorData.Add(CategoryHeader);
							}
						}

						for (i = CategoryIndex + 1, c = this.SensorData.Count; i < c; i++)
						{
							object Obj = this.SensorData[i];

							if (Obj is FieldModel FieldModel)
							{
								j = string.Compare(s, FieldModel.Name, true);
								if (j < 0)
									continue;
								else
								{
									if (j == 0)
										FieldModel.Field = Field;
									else if (j > 0)
										this.SensorData.Insert(i, new FieldModel(Field));

									break;
								}
							}
							else
							{
								this.SensorData.Insert(i, new FieldModel(Field));
								break;
							}
						}

						if (i >= c)
							this.SensorData.Add(new FieldModel(Field));
					}
				}
			});

			return Task.CompletedTask;
		}

		private Task Request_OnErrorsReceived(object Sender, IEnumerable<ThingError> NewErrors)
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
			else if (obj is FieldModel Field)
				return ViewClaimThingViewModel.LabelClicked(Field.Name, Field.ValueString, Field.ValueString, this.UiSerializer, this.LogService);
			else
				return Task.CompletedTask;
		}

	}
}
