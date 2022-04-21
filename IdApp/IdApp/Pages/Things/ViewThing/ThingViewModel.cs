using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Resx;
using IdApp.Services;
using IdApp.Services.Xmpp;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
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
			this.DisownThingCommand = new Command(async _ => await DisownThing(), _ => this.CanDisownThing);

			this.Tags = new ObservableCollection<HumanReadableTag>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out ViewThingNavigationArgs args))
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
		protected override void XmppService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
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
			BindableProperty.Create(nameof(CanDisownThing), typeof(bool), typeof(ThingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can claim a thing.
		/// </summary>
		public bool CanDisownThing
		{
			get { return this.IsConnected && (this.thing?.Owner ?? false); }
		}

		/// <summary>
		/// Command to bind to for detecting when a tag value has been clicked on.
		/// </summary>
		public ICommand ClickCommand { get; }

		/// <summary>
		/// See <see cref="IsOwner"/>
		/// </summary>
		public static readonly BindableProperty IsOwnerProperty =
			BindableProperty.Create(nameof(IsOwner), typeof(bool), typeof(ViewClaimThing.ViewClaimThingViewModel), default(bool));

		/// <summary>
		/// If the user is owner of the thing.
		/// </summary>
		public bool IsOwner
		{
			get { return (bool)this.GetValue(IsOwnerProperty); }
			set { this.SetValue(IsOwnerProperty, value); }
		}

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

				await this.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.ThingDisowned);
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