using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using IdApp.Navigation.Identity;
using IdApp.Views.Identity;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace IdApp.ViewModels.Contacts
{
	/// <summary>
	/// The view model to bind to when displaying the list of contacts.
	/// </summary>
	public class MyContactsViewModel : BaseViewModel
	{
		private readonly INeuronService neuronService;
		private readonly INetworkService networkService;
		private readonly INavigationService navigationService;
		private readonly IUiDispatcher uiDispatcher;

		/// <summary>
		/// Creates an instance of the <see cref="MyContactsViewModel"/> class.
		/// </summary>
		public MyContactsViewModel()
			: this(null, null, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="MyContactsViewModel"/> class.
		/// For unit tests.
		/// </summary>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="networkService">The network service for network access.</param>
		/// <param name="navigationService">The navigation service.</param>
		/// <param name="uiDispatcher"> The dispatcher to use for alerts and accessing the main thread.</param>
		protected internal MyContactsViewModel(INeuronService neuronService, INetworkService networkService, INavigationService navigationService, IUiDispatcher uiDispatcher)
		{
			this.neuronService = neuronService ?? Types.Instantiate<INeuronService>(false);
			this.networkService = networkService ?? Types.Instantiate<INetworkService>(false);
			this.navigationService = navigationService ?? Types.Instantiate<INavigationService>(false);
			this.uiDispatcher = uiDispatcher ?? Types.Instantiate<IUiDispatcher>(false);
			this.Contacts = new ObservableCollection<ContactInfo>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			SortedDictionary<string, ContactInfo> Sorted = new SortedDictionary<string, ContactInfo>();
			string Name;
			string Suffix;
			int i;

			foreach (ContactInfo Info in await Database.Find<ContactInfo>(new FilterNot(new FilterFieldEqualTo("IsThing", true))))
			{
				Name = Info.FriendlyName;
				if (Sorted.ContainsKey(Name))
				{
					i = 1;

					do
					{
						Suffix = " " + (++i).ToString();
					}
					while (Sorted.ContainsKey(Name + Suffix));

					Sorted[Name + Suffix] = Info;
				}
				else
					Sorted[Name] = Info;
			}

			this.Contacts.Clear();
			foreach (ContactInfo Info in Sorted.Values)
				this.Contacts.Add(Info);

			this.ShowContactsMissing = Sorted.Count == 0;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.ShowContactsMissing = false;
			this.Contacts.Clear();
			await base.DoUnbind();
		}

		/// <summary>
		/// See <see cref="ShowContactsMissing"/>
		/// </summary>
		public static readonly BindableProperty ShowContactsMissingProperty =
			BindableProperty.Create("ShowContactsMissing", typeof(bool), typeof(MyContactsViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether to show a contacts missing alert or not.
		/// </summary>
		public bool ShowContactsMissing
		{
			get { return (bool)GetValue(ShowContactsMissingProperty); }
			set { SetValue(ShowContactsMissingProperty, value); }
		}

		/// <summary>
		/// Holds the list of contacts to display.
		/// </summary>
		public ObservableCollection<ContactInfo> Contacts { get; }

		/// <summary>
		/// See <see cref="SelectedContact"/>
		/// </summary>
		public static readonly BindableProperty SelectedContactProperty =
			BindableProperty.Create("SelectedContact", typeof(ContactInfo), typeof(MyContactsViewModel), default(ContactInfo), propertyChanged: (b, oldValue, newValue) =>
			{
				if (b is MyContactsViewModel viewModel &&
					newValue is ContactInfo Contact)
				{
					viewModel.uiDispatcher.BeginInvokeOnMainThread(async () =>
					{
						await viewModel.navigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Contact.LegalIdentity, null));
					});
				}
			});

		/// <summary>
		/// The currently selected contact, if any.
		/// </summary>
		public ContactInfo SelectedContact
		{
			get { return (ContactInfo)GetValue(SelectedContactProperty); }
			set { SetValue(SelectedContactProperty, value); }
		}

	}
}