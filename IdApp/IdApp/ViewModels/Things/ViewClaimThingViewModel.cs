using IdApp.Navigation;
using IdApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Provisioning;
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
            ILogService logService,
            IImageCacheService imageCacheService)
        : base(neuronService, uiDispatcher)
        {
            this.tagProfile = tagProfile;
            this.logService = logService;
            this.navigationService = navigationService;
            this.networkService = networkService;
            imageCacheService = imageCacheService ?? DependencyService.Resolve<IImageCacheService>();

            this.ClaimThingCommand = new Command(async _ => await ClaimThing(), _ => IsConnected);
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
                        this.Tags.Add(new HumanReadableTag(Tag, this));
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
            get { return (string) GetValue(PinProperty); }
            set { SetValue(PinProperty, value); }
        }

        #endregion

        /// <summary>
        /// Called when tag value has been clicked.
        /// </summary>
        internal async void LabelClicked(HumanReadableTag Tag)
        {
            try
            {
                //await Clipboard.SetTextAsync($"iotid:{LegalId}");
                //await UiDispatcher.DisplayAlert("Copied", "Your ID was copied to clipboard.");
            }
            catch (Exception ex)
            {
                //logService.LogException(ex);
                //await UiDispatcher.DisplayAlert(ex);
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

                bool succeeded = await this.networkService.TryRequest(() => this.NeuronService.ThingRegistry.ClaimThing(this.Uri, this.MakePublic));
                if (succeeded)
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