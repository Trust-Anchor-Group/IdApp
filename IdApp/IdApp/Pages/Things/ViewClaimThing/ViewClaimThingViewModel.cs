using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Things.ViewClaimThing
{
	/// <summary>
	/// The view model to bind to for when displaying thing claim information.
	/// </summary>
	public class ViewClaimThingViewModel : NeuronViewModel
	{
		private readonly ILogService logService;
		private readonly INavigationService navigationService;
		private readonly INetworkService networkService;

		/// <summary>
		/// Creates an instance of the <see cref="ViewClaimThingViewModel"/> class.
		/// </summary>
		public ViewClaimThingViewModel(
			ITagProfile tagProfile,
			IUiSerializer uiSerializer,
			INeuronService neuronService,
			INavigationService navigationService,
			INetworkService networkService,
			ILogService logService)
			: base(neuronService, uiSerializer, tagProfile)
		{
			this.logService = logService;
			this.navigationService = navigationService;
			this.networkService = networkService;

			this.ClickCommand = new Command(async x => await this.LabelClicked(x));
			this.ClaimThingCommand = new Command(async _ => await ClaimThing(), _ => this.CanClaimThing);
			this.Tags = new ObservableCollection<HumanReadableTag>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out ViewClaimThingNavigationArgs args))
			{
				this.Uri = args.Uri;

				if (this.NeuronService.ThingRegistry.TryDecodeIoTDiscoClaimURI(args.Uri, out MetaDataTag[] Tags))
				{
					foreach (MetaDataTag Tag in Tags)
						this.Tags.Add(new HumanReadableTag(Tag));
				}
			}

			AssignProperties();
			EvaluateAllCommands();

			this.TagProfile.Changed += TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.TagProfile.Changed -= TagProfile_Changed;
			await base.DoUnbind();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.ClaimThingCommand);
		}

		/// <inheritdoc/>
		protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(e.State);
				this.EvaluateAllCommands();
			});
		}

		private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(AssignProperties);
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
			BindableProperty.Create("Uri", typeof(string), typeof(ViewClaimThingViewModel), default(string));

		/// <summary>
		/// iotdisco URI to process
		/// </summary>
		public string Uri
		{
			get { return (string)GetValue(UriProperty); }
			set { SetValue(UriProperty, value); }
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
			BindableProperty.Create("CanClaimThing", typeof(bool), typeof(ViewClaimThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can claim a thing.
		/// </summary>
		public bool CanClaimThing
		{
			get { return this.IsConnected && this.NeuronService.IsOnline; }
		}

		/// <summary>
		/// See <see cref="MakePublic"/>
		/// </summary>
		public static readonly BindableProperty MakePublicProperty =
			BindableProperty.Create("MakePublic", typeof(bool), typeof(ViewClaimThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can claim a thing.
		/// </summary>
		public bool MakePublic
		{
			get { return (bool)GetValue(MakePublicProperty); }
			set { SetValue(MakePublicProperty, value); }
		}

		#endregion

		private Task LabelClicked(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this.UiSerializer, this.logService);
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
				await uiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.TagValueCopiedToClipboard);
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

				(bool Succeeded, NodeResultEventArgs e) = await this.networkService.TryRequest(() => this.NeuronService.ThingRegistry.ClaimThing(this.Uri, this.MakePublic));
				if (!Succeeded)
					return;

				if (e.Ok)
				{
					string FriendlyName = GetFriendlyName(this.Tags);
					RosterItem Item = this.NeuronService.Xmpp[e.JID];
					if (Item is null)
						this.NeuronService.Xmpp.AddRosterItem(new RosterItem(e.JID, FriendlyName));

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
					await this.navigationService.GoBackAsync();
				}
				else
				{
					string Msg = e.ErrorText;
					if (string.IsNullOrEmpty(Msg))
						Msg = AppResources.UnableToClaimThing;

					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, Msg);
				}
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
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
			List<Property> Result = new List<Property>();

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
			List<Property> Result = new List<Property>();

			foreach (MetaDataTag Tag in Tags)
				Result.Add(new Property(Tag.Name, Tag.StringValue));

			return Result.ToArray();
		}

	}
}