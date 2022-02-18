using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using IdApp.Pages.Things.ViewThing;
using IdApp.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Xamarin.Forms;

namespace IdApp.Pages.Things.MyThings
{
	/// <summary>
	/// The view model to bind to when displaying the list of things.
	/// </summary>
	public class MyThingsViewModel : BaseViewModel
	{
		private TaskCompletionSource<ContactInfo> thingToShare;

		/// <summary>
		/// Creates an instance of the <see cref="MyThingsViewModel"/> class.
		/// </summary>
		protected internal MyThingsViewModel()
		{
			this.Things = new ObservableCollection<ContactInfo>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out MyThingsNavigationArgs Args))
				this.thingToShare = Args.ThingToShare;
			else
				this.thingToShare = null;

			SortedDictionary<string, ContactInfo> SortedByName = new SortedDictionary<string, ContactInfo>();
			SortedDictionary<string, ContactInfo> SortedByAddress = new SortedDictionary<string, ContactInfo>();
			string Name;
			string Suffix;
			string Key;
			int i;

			foreach (ContactInfo Info in await Database.Find<ContactInfo>(new FilterFieldEqualTo("IsThing", true)))
			{
				Name = Info.FriendlyName;
				if (SortedByName.ContainsKey(Name))
				{
					i = 1;

					do
					{
						Suffix = " " + (++i).ToString();
					}
					while (SortedByName.ContainsKey(Name + Suffix));

					SortedByName[Name + Suffix] = Info;
				}
				else
					SortedByName[Name] = Info;

				Key = Info.BareJid + ", " + Info.SourceId + ", " + Info.Partition + ", " + Info.NodeId;
				SortedByAddress[Key] = Info;
			}

			SearchResultThing[] MyDevices = await this.XmppService.Provisioning.GetAllMyDevices();
			foreach (SearchResultThing Thing in MyDevices)
			{
				Property[] MetaData = ViewClaimThing.ViewClaimThingViewModel.ToProperties(Thing.Tags);

				Key = Thing.Jid + ", " + Thing.Node.SourceId + ", " + Thing.Node.Partition + ", " + Thing.Node.NodeId;
				if (SortedByAddress.TryGetValue(Key, out ContactInfo Info))
				{
					if (!Info.Owner.HasValue || !Info.Owner.Value || !AreSame(Info.MetaData, MetaData))
					{
						Info.Owner = true;
						Info.MetaData = MetaData;
						Info.FriendlyName = ViewClaimThing.ViewClaimThingViewModel.GetFriendlyName(MetaData);

						await Database.Update(Info);
					}

					continue;
				}

				Info = new ContactInfo()
				{
					BareJid = Thing.Jid,
					LegalId = string.Empty,
					LegalIdentity = null,
					FriendlyName = ViewClaimThing.ViewClaimThingViewModel.GetFriendlyName(Thing.Tags),
					IsThing = true,
					SourceId = Thing.Node.SourceId,
					Partition = Thing.Node.Partition,
					NodeId = Thing.Node.NodeId,
					Owner = true,
					MetaData = MetaData,
					RegistryJid = this.XmppService.Provisioning.ServiceJid
				};

				await Database.Insert(Info);

				Name = Info.FriendlyName;
				if (SortedByName.ContainsKey(Name))
				{
					i = 1;

					do
					{
						Suffix = " " + (++i).ToString();
					}
					while (SortedByName.ContainsKey(Name + Suffix));

					SortedByName[Name + Suffix] = Info;
				}
				else
					SortedByName[Name] = Info;

				SortedByAddress[Key] = Info;
			}

			this.Things.Clear();
			foreach (ContactInfo Info in SortedByName.Values)
				this.Things.Add(Info);

			this.ShowThingsMissing = SortedByName.Count == 0;
		
			await Database.Provider.Flush();
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.ShowThingsMissing = false;
			this.Things.Clear();

			await base.DoUnbind();

			this.thingToShare?.TrySetResult(null);
		}

		/// <summary>
		/// Checks to sets of meta-data about a thing, to see if they match.
		/// </summary>
		/// <param name="MetaData1">First set of meta-data.</param>
		/// <param name="MetaData2">Second set of meta-data.</param>
		/// <returns>If they are the same.</returns>
		public static bool AreSame(Property[] MetaData1, Property[] MetaData2)
		{
			if ((MetaData1 is null) ^ (MetaData2 is null))
				return false;

			if (MetaData1 is null)
				return true;

			int i, c = MetaData1.Length;
			if (MetaData2.Length != c)
				return false;

			for (i = 0; i < c; i++)
			{
				if ((MetaData1[i].Name != MetaData2[i].Name) || (MetaData1[i].Value != MetaData2[i].Value))
					return false;
			}

			return true;
		}

		/// <summary>
		/// See <see cref="ShowThingsMissing"/>
		/// </summary>
		public static readonly BindableProperty ShowThingsMissingProperty =
			BindableProperty.Create("ShowThingsMissing", typeof(bool), typeof(MyThingsViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether to show a contacts missing alert or not.
		/// </summary>
		public bool ShowThingsMissing
		{
			get { return (bool)GetValue(ShowThingsMissingProperty); }
			set { SetValue(ShowThingsMissingProperty, value); }
		}

		/// <summary>
		/// Holds the list of contacts to display.
		/// </summary>
		public ObservableCollection<ContactInfo> Things { get; }

		/// <summary>
		/// See <see cref="SelectedThing"/>
		/// </summary>
		public static readonly BindableProperty SelectedThingProperty =
			BindableProperty.Create("SelectedThing", typeof(ContactInfo), typeof(MyThingsViewModel), default(ContactInfo), propertyChanged: (b, oldValue, newValue) =>
			{
				if (b is MyThingsViewModel viewModel && newValue is ContactInfo Thing)
				{
					viewModel.UiSerializer.BeginInvokeOnMainThread(async () =>
					{
						if (viewModel.thingToShare is null)
							await viewModel.NavigationService.GoToAsync(nameof(ViewThingPage), new ViewThingNavigationArgs(Thing));
						else
						{
							viewModel.thingToShare.TrySetResult(Thing);
							await viewModel.NavigationService.GoBackAsync();
						}
					});
				}
			});

		/// <summary>
		/// The currently selected contact, if any.
		/// </summary>
		public ContactInfo SelectedThing
		{
			get { return (ContactInfo)GetValue(SelectedThingProperty); }
			set { SetValue(SelectedThingProperty, value); }
		}

	}
}