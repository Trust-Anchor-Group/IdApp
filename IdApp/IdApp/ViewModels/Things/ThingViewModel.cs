using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Navigation.Things;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace IdApp.ViewModels.Things
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public class ThingViewModel : NeuronViewModel
	{
		private readonly ITagProfile tagProfile;
		private readonly INetworkService networkService;
		private readonly INavigationService navigationService;
		private readonly ILogService logService;
		private ContactInfo thing;

		/// <summary>
		/// Creates an instance of the <see cref="ThingViewModel"/> class.
		/// </summary>
		public ThingViewModel()
			: this(null, null, null, null, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ThingViewModel"/> class.
		/// For unit tests.
		/// </summary>
		/// <param name="tagProfile">TAG Profile.</param>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="networkService">The network service for network access.</param>
		/// <param name="navigationService">The navigation service.</param>
		/// <param name="uiDispatcher"> The dispatcher to use for alerts and accessing the main thread.</param>
		/// <param name="logService">Log service.</param>
		protected internal ThingViewModel(
			ITagProfile tagProfile,
			INeuronService neuronService,
			INetworkService networkService,
			INavigationService navigationService,
			IUiDispatcher uiDispatcher,
			ILogService logService)
			: base(neuronService, uiDispatcher)
		{
			this.tagProfile = tagProfile ?? Types.Instantiate<ITagProfile>(false);
			this.networkService = networkService ?? Types.Instantiate<INetworkService>(false);
			this.navigationService = navigationService ?? Types.Instantiate<INavigationService>(false);
			this.logService = logService ?? Types.Instantiate<ILogService>(false);

			this.ClickCommand = new Command(async x => await this.LabelClicked(x));
			this.DisownThingCommand = new Command(async _ => await DisownThing(), _ => this.CanDisownThing);

			this.Tags = new ObservableCollection<HumanReadableTag>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out ViewThingNavigationArgs args))
			{
				this.thing = args.Thing;

				foreach (Property Tag in this.thing.MetaData)
					this.Tags.Add(new HumanReadableTag(Tag));
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
			this.EvaluateCommands(this.DisownThingCommand);
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
		/// Holds a list of meta-data tags associated with a thing.
		/// </summary>
		public ObservableCollection<HumanReadableTag> Tags { get; }

		/// <summary>
		/// The command to bind to for claiming a thing
		/// </summary>
		public ICommand DisownThingCommand { get; }

		/// <summary>
		/// See <see cref="CanDisownThing"/>
		/// </summary>
		public static readonly BindableProperty CanDisownThingProperty =
			BindableProperty.Create("CanDisownThing", typeof(bool), typeof(ThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can claim a thing.
		/// </summary>
		public bool CanDisownThing
		{
			get { return this.NeuronService.IsOnline && IsConnected && (this.thing?.Owner ?? false); }
		}

		/// <summary>
		/// If PIN should be used.
		/// </summary>
		public bool UsePin => this.tagProfile?.UsePin ?? false;

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

		/// <summary>
		/// Command to bind to for detecting when a tag value has been clicked on.
		/// </summary>
		public ICommand ClickCommand { get; }

		/// <summary>
		/// See <see cref="IsOwner"/>
		/// </summary>
		public static readonly BindableProperty IsOwnerProperty =
			BindableProperty.Create("IsOwner", typeof(bool), typeof(ViewClaimThingViewModel), default(bool));

		/// <summary>
		/// If the user is owner of the thing.
		/// </summary>
		public bool IsOwner
		{
			get { return (bool)GetValue(IsOwnerProperty); }
			set { SetValue(IsOwnerProperty, value); }
		}

		#endregion

		private Task LabelClicked(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return ViewClaimThingViewModel.LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this.UiDispatcher, this.logService);
			else
				return Task.CompletedTask;
		}

		private async Task DisownThing()
		{
			try
			{
				if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
				{
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
					return;
				}

				(bool Succeeded, bool Done) = await this.networkService.TryRequest(() => 
					this.NeuronService.ThingRegistry.Disown(this.thing.RegistryJid, this.thing.BareJid, this.thing.SourceId, this.thing.Partition, this.thing.NodeId));

				if (!Succeeded)
					return;

				if (!string.IsNullOrEmpty(this.thing.ObjectId))
					await Database.Delete(this.thing);

				await this.UiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.ThingDisowned);
				await this.navigationService.GoBackAsync();
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}

	}
}