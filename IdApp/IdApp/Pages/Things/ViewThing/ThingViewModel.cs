using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services;
using IdApp.Services.Xmpp;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Things.ViewThing
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public class ThingViewModel : XmppViewModel
	{
		private ContactInfo thing;

		/// <summary>
		/// Creates an instance of the <see cref="ThingViewModel"/> class.
		/// </summary>
		protected internal ThingViewModel()
			: base()
		{
			this.ClickCommand = new Command(async x => await this.LabelClicked(x));
			this.DisownThingCommand = new Command(async _ => await this.DisownThing(), _ => this.IsConnected && this.IsOwner);

			this.Tags = new ObservableCollection<HumanReadableTag>();
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
					foreach (Property Tag in this.thing.MetaData)
						this.Tags.Add(new HumanReadableTag(Tag));
				}

				this.InContacts = !string.IsNullOrEmpty(this.thing.ObjectId);
				this.IsOwner = this.thing.Owner ?? false;
			}

			this.AssignProperties();
			this.EvaluateAllCommands();

			this.XmppService.Xmpp.OnPresence += this.Xmpp_OnPresence;
			this.TagProfile.Changed += this.TagProfile_Changed;

			if (this.IsConnected && this.IsThingOnline)
			{
				bool ContactInfoChanged = false;

				if (!this.thing.IsSensor.HasValue)
				{
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
			RosterItem Item = this.XmppService.Xmpp[this.thing.BareJid];
			this.IsThingOnline = Item is not null && Item.HasLastPresence && Item.LastPresence.IsOnline;
		}

		private void AssignProperties()
		{
			this.CalcThingIsOnline();
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.DisownThingCommand);
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
		/// The command to bind to for claiming a thing
		/// </summary>
		public ICommand DisownThingCommand { get; }

		/// <summary>
		/// See <see cref="InContacts"/>
		/// </summary>
		public static readonly BindableProperty InContactsProperty =
			BindableProperty.Create(nameof(InContacts), typeof(bool), typeof(ThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact list.
		/// </summary>
		public bool InContacts
		{
			get => (bool)this.GetValue(InContactsProperty);
			set => this.SetValue(InContactsProperty, value);
		}

		/// <summary>
		/// See <see cref="IsOwner"/>
		/// </summary>
		public static readonly BindableProperty IsOwnerProperty =
			BindableProperty.Create(nameof(IsOwner), typeof(bool), typeof(ThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact.
		/// </summary>
		public bool IsOwner
		{
			get => (bool)this.GetValue(IsOwnerProperty);
			set => this.SetValue(IsOwnerProperty, value);
		}

		/// <summary>
		/// See <see cref="IsThingOnline"/>
		/// </summary>
		public static readonly BindableProperty IsThingOnlineProperty =
			BindableProperty.Create(nameof(IsThingOnline), typeof(bool), typeof(ThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the thing is in the contact.
		/// </summary>
		public bool IsThingOnline
		{
			get => (bool)this.GetValue(IsThingOnlineProperty);
			set => this.SetValue(IsThingOnlineProperty, value);
		}

		/// <summary>
		/// Command to bind to for detecting when a tag value has been clicked on.
		/// </summary>
		public ICommand ClickCommand { get; }

		#endregion

		private Task LabelClicked(object obj)
		{
			if (obj is HumanReadableTag Tag)
				return ViewClaimThing.ViewClaimThingViewModel.LabelClicked(Tag.Name, Tag.Value, Tag.LocalizedValue, this.UiSerializer, this.LogService);
			else
				return Task.CompletedTask;
		}

		private async Task DisownThing()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				(bool Succeeded, bool Done) = await this.NetworkService.TryRequest(() => 
					this.XmppService.ThingRegistry.Disown(this.thing.RegistryJid, this.thing.BareJid, this.thing.SourceId, this.thing.Partition, this.thing.NodeId));

				if (!Succeeded)
					return;

				if (!string.IsNullOrEmpty(this.thing.ObjectId))
				{
					await Database.Delete(this.thing);
					await Database.Provider.Flush();
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

	}
}
