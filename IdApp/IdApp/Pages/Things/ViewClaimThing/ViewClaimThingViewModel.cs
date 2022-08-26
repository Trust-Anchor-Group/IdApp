using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services;
using IdApp.Services.EventLog;
using IdApp.Services.Xmpp;
using IdApp.Services.UI;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Things.ViewClaimThing
{
	/// <summary>
	/// The view model to bind to for when displaying thing claim information.
	/// </summary>
	public class ViewClaimThingViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="ViewClaimThingViewModel"/> class.
		/// </summary>
		public ViewClaimThingViewModel()
			: base()
		{
			this.ClickCommand = new Command(async x => await this.LabelClicked(x));
			this.ClaimThingCommand = new Command(async _ => await this.ClaimThing(), _ => this.CanClaimThing);
			this.Tags = new ObservableCollection<HumanReadableTag>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out ViewClaimThingNavigationArgs args))
			{
				this.Uri = args.Uri;

				if (this.XmppService.ThingRegistry.TryDecodeIoTDiscoClaimURI(args.Uri, out MetaDataTag[] Tags))
				{
					foreach (MetaDataTag Tag in Tags)
						this.Tags.Add(new HumanReadableTag(Tag));
				}
			}

			this.AssignProperties();
			this.EvaluateAllCommands();

			this.TagProfile.Changed += this.TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.TagProfile.Changed -= this.TagProfile_Changed;

			await base.OnDispose();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.ClaimThingCommand);
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
		/// Command to bind to for detecting when a tag value has been clicked on.
		/// </summary>
		public ICommand ClickCommand { get; }

		/// <summary>
		/// See <see cref="Uri"/>
		/// </summary>
		public static readonly BindableProperty UriProperty =
			BindableProperty.Create(nameof(Uri), typeof(string), typeof(ViewClaimThingViewModel), default(string));

		/// <summary>
		/// iotdisco URI to process
		/// </summary>
		public string Uri
		{
			get => (string)this.GetValue(UriProperty);
			set => this.SetValue(UriProperty, value);
		}

		/// <summary>
		/// Holds a list of meta-data tags associated with a thing.
		/// </summary>
		public ObservableCollection<HumanReadableTag> Tags { get; }

		/// <summary>
		/// The command to bind to for claiming a thing
		/// </summary>
		public ICommand ClaimThingCommand { get; }

		/// <summary>
		/// See <see cref="CanClaimThing"/>
		/// </summary>
		public static readonly BindableProperty CanClaimThingProperty =
			BindableProperty.Create(nameof(CanClaimThing), typeof(bool), typeof(ViewClaimThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can claim a thing.
		/// </summary>
		public bool CanClaimThing
		{
			get { return this.IsConnected && this.XmppService.IsOnline; }
		}

		/// <summary>
		/// See <see cref="MakePublic"/>
		/// </summary>
		public static readonly BindableProperty MakePublicProperty =
			BindableProperty.Create(nameof(MakePublic), typeof(bool), typeof(ViewClaimThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can claim a thing.
		/// </summary>
		public bool MakePublic
		{
			get => (bool)this.GetValue(MakePublicProperty);
			set => this.SetValue(MakePublicProperty, value);
		}

		#endregion

		private Task LabelClicked(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this.UiSerializer, this.LogService);
			else
				return Task.CompletedTask;
		}

		/// <summary>
		/// Processes the click of a localized meta-data label.
		/// </summary>
		/// <param name="Name">Tag name</param>
		/// <param name="Value">Tag value</param>
		/// <param name="LocalizedValue">Localized tag value</param>
		/// <param name="uiSerializer">UI Dispatcher service</param>
		/// <param name="logService">Log service</param>
		public static async Task LabelClicked(string Name, string Value, string LocalizedValue, IUiSerializer uiSerializer, ILogService logService)
		{ 
			try
			{
				switch (Name)
				{
					case "MAN":
						if (System.Uri.TryCreate("https://" + Value, UriKind.Absolute, out Uri Uri) && await Launcher.TryOpenAsync(Uri))
							return;
						break;

					case "PURL":
						if (System.Uri.TryCreate(Value, UriKind.Absolute, out Uri) && await Launcher.TryOpenAsync(Uri))
							return;
						break;

					case "R":
						SRV SRV;

						try
						{
							SRV = await DnsResolver.LookupServiceEndpoint(Value, "xmpp-server", "tcp");
						}
						catch
						{
							break;
						}

						if (System.Uri.TryCreate("https://" + SRV.TargetHost, UriKind.Absolute, out Uri) && await Launcher.TryOpenAsync(Uri))
							return;
						break;

					default:
						if ((Value.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase) ||
							Value.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase)) &&
							System.Uri.TryCreate(Value, UriKind.Absolute, out Uri) && await Launcher.TryOpenAsync(Uri))
						{
							return;
						}
						break;
				}

				await Clipboard.SetTextAsync(LocalizedValue);
				await uiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["TagValueCopiedToClipboard"]);
			}
			catch (Exception ex)
			{
				logService.LogException(ex);
				await uiSerializer.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// Get Friendly name of thing
		/// </summary>
		public static string GetFriendlyName(IEnumerable<HumanReadableTag> Tags)
		{
			return GetFriendlyName(ToProperties(Tags));
		}

		/// <summary>
		/// Get Friendly name of thing
		/// </summary>
		public static string GetFriendlyName(IEnumerable<MetaDataTag> Tags)
		{
			return GetFriendlyName(ToProperties(Tags));
		}

		/// <summary>
		/// Get Friendly name of thing
		/// </summary>
		public static string GetFriendlyName(IEnumerable<Property> Tags)
		{
			return ContactInfo.GetFriendlyName(Tags);
		}

		private async Task ClaimThing()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				(bool Succeeded, NodeResultEventArgs e) = await this.NetworkService.TryRequest(() => this.XmppService.ThingRegistry.ClaimThing(this.Uri, this.MakePublic));
				if (!Succeeded)
					return;

				if (e.Ok)
				{
					string FriendlyName = GetFriendlyName(this.Tags);
					RosterItem Item = this.XmppService.Xmpp[e.JID];
					if (Item is null)
						this.XmppService.Xmpp.AddRosterItem(new RosterItem(e.JID, FriendlyName));

					ContactInfo Info = await ContactInfo.FindByBareJid(e.JID, e.Node.SourceId, e.Node.Partition, e.Node.NodeId);
					if (Info is null)
					{
						Info = new ContactInfo()
						{
							BareJid = e.JID,
							LegalId = string.Empty,
							LegalIdentity = null,
							FriendlyName = FriendlyName,
							IsThing = true,
							Owner = true,
							SourceId = e.Node.SourceId,
							Partition = e.Node.Partition,
							NodeId = e.Node.NodeId,
							MetaData = ToProperties(this.Tags),
							RegistryJid = e.From
						};

						await Database.Insert(Info);
					}
					else
					{
						Info.FriendlyName = FriendlyName;

						await Database.Update(Info);
					}

					await Database.Provider.Flush();
					await this.NavigationService.GoBackAsync();
				}
				else
				{
					string Msg = e.ErrorText;
					if (string.IsNullOrEmpty(Msg))
						Msg = LocalizationResourceManager.Current["UnableToClaimThing"];

					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], Msg);
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// Converts an enumerable set of <see cref="HumanReadableTag"/> to an enumerable set of <see cref="Property"/>.
		/// </summary>
		/// <param name="Tags">Enumerable set of <see cref="HumanReadableTag"/></param>
		/// <returns>Enumerable set of <see cref="Property"/></returns>
		public static Property[] ToProperties(IEnumerable<HumanReadableTag> Tags)
		{
			List<Property> Result = new();

			foreach (HumanReadableTag Tag in Tags)
				Result.Add(new Property(Tag.Name, Tag.Value));

			return Result.ToArray();
		}

		/// <summary>
		/// Converts an enumerable set of <see cref="MetaDataTag"/> to an enumerable set of <see cref="Property"/>.
		/// </summary>
		/// <param name="Tags">Enumerable set of <see cref="MetaDataTag"/></param>
		/// <returns>Enumerable set of <see cref="Property"/></returns>
		public static Property[] ToProperties(IEnumerable<MetaDataTag> Tags)
		{
			List<Property> Result = new();

			foreach (MetaDataTag Tag in Tags)
				Result.Add(new Property(Tag.Name, Tag.StringValue));

			return Result.ToArray();
		}

	}
}
