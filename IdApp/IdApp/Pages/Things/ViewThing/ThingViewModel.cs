using System;
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
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Xamarin.Forms;

namespace IdApp.Pages.Things.ViewThing
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public class ThingViewModel : NeuronViewModel
	{
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
			IUiSerializer uiDispatcher,
			ILogService logService)
			: base(neuronService, uiDispatcher, tagProfile)
		{
			this.networkService = networkService ?? App.Instantiate<INetworkService>();
			this.navigationService = navigationService ?? App.Instantiate<INavigationService>();
			this.logService = logService ?? App.Instantiate<ILogService>();

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
		public bool UsePin => this.TagProfile?.UsePin ?? false;

		/// <summary>
		/// See <see cref="Pin"/>
		/// </summary>
		public static readonly BindableProperty PinProperty =
			BindableProperty.Create("Pin", typeof(string), typeof(ViewClaimThing.ViewClaimThingViewModel), default(string));

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
			BindableProperty.Create("IsOwner", typeof(bool), typeof(ViewClaimThing.ViewClaimThingViewModel), default(bool));

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
				return ViewClaimThing.ViewClaimThingViewModel.LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this.UiDispatcher, this.logService);
			else
				return Task.CompletedTask;
		}

		private async Task DisownThing()
		{
			try
			{
				if (this.TagProfile.UsePin && this.TagProfile.ComputePinHash(this.Pin) != this.TagProfile.PinHash)
				{
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
					return;
				}

				(bool Succeeded, bool Done) = await this.networkService.TryRequest(() => 
					this.NeuronService.ThingRegistry.Disown(this.thing.RegistryJid, this.thing.BareJid, this.thing.SourceId, this.thing.Partition, this.thing.NodeId));

				if (!Succeeded)
					return;

				if (!string.IsNullOrEmpty(this.thing.ObjectId))
				{
					await Database.Delete(this.thing);
					await Database.Provider.Flush();
				}

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