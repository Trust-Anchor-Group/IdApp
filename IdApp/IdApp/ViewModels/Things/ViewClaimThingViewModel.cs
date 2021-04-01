using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Navigation.Things;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.ViewModels.Things
{
	/// <summary>
	/// The view model to bind to for when displaying thing claim information.
	/// </summary>
	public class ViewClaimThingViewModel : NeuronViewModel
	{
		private readonly ITagProfile tagProfile;
		private readonly ILogService logService;
		private readonly INavigationService navigationService;
		private readonly INetworkService networkService;

		/// <summary>
		/// Creates an instance of the <see cref="ViewClaimThingViewModel"/> class.
		/// </summary>
		public ViewClaimThingViewModel(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			INetworkService networkService,
			ILogService logService)
		: base(neuronService, uiDispatcher)
		{
			this.tagProfile = tagProfile;
			this.logService = logService;
			this.navigationService = navigationService;
			this.networkService = networkService;

			this.ClaimThingCommand = new Command(async _ => await ClaimThing(), _ => IsConnected);
			this.Tags = new ObservableCollection<HumanReadableTag>();

			this.ClickCommand = new Command(async x => await this.LabelClicked(x));
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

			this.tagProfile.Changed += TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.tagProfile.Changed -= TagProfile_Changed;
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
			this.UiDispatcher.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(e.State);
				this.EvaluateAllCommands();
			});
		}

		private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
		{
			this.UiDispatcher.BeginInvokeOnMainThread(AssignProperties);
		}

		#region Properties

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
			get { return this.NeuronService.State == XmppState.Connected; }
		}

		/// <summary>
		/// If PIN should be used.
		/// </summary>
		public bool UsePin => this.tagProfile?.UsePin ?? false;

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

		/// <summary>
		/// See <see cref="Pin"/>
		/// </summary>
		public static readonly BindableProperty PinProperty =
			BindableProperty.Create("Pin", typeof(string), typeof(ViewClaimThingViewModel), default(string));

		/// <summary>
		/// Gets or sets the PIN code for the identity.
		/// </summary>
		public string Pin
		{
			get { return (string)GetValue(PinProperty); }
			set { SetValue(PinProperty, value); }
		}

		#endregion

		/// <summary>
		/// Command to bind to for detecting when a tag value has been clicked on.
		/// </summary>
		public ICommand ClickCommand { get; }

		private async Task LabelClicked(object obj)
		{
			HumanReadableTag Tag = (HumanReadableTag)obj;

			try
			{
				string Name = Tag.Tag.Name;
				string Value = Tag.Tag.StringValue;

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

				await Clipboard.SetTextAsync(Tag.StringValue);
				await UiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.TagValueCopiedToClipboard);
			}
			catch (Exception ex)
			{
				logService.LogException(ex);
				await UiDispatcher.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// Get Friendly name of thing
		/// </summary>
		public static string GetFriendlyName(IEnumerable<HumanReadableTag> Tags)
		{
			string APT = null;
			string AREA = null;
			string BLD = null;
			string CITY = null;
			string CLASS = null;
			string COUNTRY = null;
			string MAN = null;
			string MLOC = null;
			string MNR = null;
			string MODEL = null;
			string NAME = null;
			string REGION = null;
			string ROOM = null;
			string SN = null;
			string STREET = null;
			string STREETNR = null;
			string V = null;

			foreach (HumanReadableTag Tag in Tags)
			{
				switch (Tag.Name)
				{
					case "APT": APT = Tag.StringValue; break;
					case "AREA": AREA = Tag.StringValue; break;
					case "BLD": BLD = Tag.StringValue; break;
					case "CITY": CITY = Tag.StringValue; break;
					case "CLASS": CLASS = Tag.StringValue; break;
					case "COUNTRY": COUNTRY = Tag.StringValue; break;
					case "MAN": MAN = Tag.StringValue; break;
					case "MLOC": MLOC = Tag.StringValue; break;
					case "MNR": MNR = Tag.StringValue; break;
					case "MODEL": MODEL = Tag.StringValue; break;
					case "NAME": NAME = Tag.StringValue; break;
					case "REGION": REGION = Tag.StringValue; break;
					case "ROOM": ROOM = Tag.StringValue; break;
					case "SN": SN = Tag.StringValue; break;
					case "STREET": STREET = Tag.StringValue; break;
					case "STREETNR": STREETNR = Tag.StringValue; break;
					case "V": V = Tag.StringValue; break;
				}
			}

			StringBuilder sb = new StringBuilder();
			bool First = true;

			if (!string.IsNullOrEmpty(STREETNR))
			{
				if (string.IsNullOrEmpty(STREET))
					STREET = STREETNR;
				else
					STREET += " " + STREETNR;
			}

			Append(sb, ref First, NAME);
			Append(sb, ref First, CLASS);
			Append(sb, ref First, MAN);
			Append(sb, ref First, MODEL);
			Append(sb, ref First, V);
			Append(sb, ref First, SN);
			Append(sb, ref First, MLOC);
			Append(sb, ref First, MNR);
			Append(sb, ref First, ROOM);
			Append(sb, ref First, APT);
			Append(sb, ref First, BLD);
			Append(sb, ref First, STREET);
			Append(sb, ref First, AREA);
			Append(sb, ref First, REGION);
			Append(sb, ref First, CITY);
			Append(sb, ref First, COUNTRY);

			return sb.ToString();
		}

		private static void Append(StringBuilder sb, ref bool First, string Value)
		{
			if (!string.IsNullOrEmpty(Value))
			{
				if (First)
					First = false;
				else
					sb.Append(", ");

				sb.Append(Value);
			}
		}

		private async Task ClaimThing()
		{
			try
			{
				if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
				{
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
					return;
				}

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
							SourceId = e.Node.SourceId,
							Partition = e.Node.Partition,
							NodeId = e.Node.NodeId,
							RegistryJid = e.From
						};

						await Database.Insert(Info);
					}
					else
					{
						Info.FriendlyName = FriendlyName;

						await Database.Update(Info);
					}

					await this.navigationService.GoBackAsync();
				}
				else
				{
					string Msg = e.ErrorText;
					if (string.IsNullOrEmpty(Msg))
						Msg = AppResources.UnableToClaimThing;

					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, Msg);
				}
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}
	}
}