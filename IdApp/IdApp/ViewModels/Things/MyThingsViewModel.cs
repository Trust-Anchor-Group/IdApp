using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using IdApp.Navigation.Things;
using IdApp.Views.Things;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Xamarin.Forms;

namespace IdApp.ViewModels.Things
{
	/// <summary>
	/// The view model to bind to when displaying the list of things.
	/// </summary>
	public class MyThingsViewModel : BaseViewModel
	{
		private readonly INeuronService neuronService;
		private readonly INetworkService networkService;
		private readonly INavigationService navigationService;
		private readonly IUiDispatcher uiDispatcher;

		/// <summary>
		/// Creates an instance of the <see cref="MyThingsViewModel"/> class.
		/// </summary>
		public MyThingsViewModel()
			: this(null, null, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="MyThingsViewModel"/> class.
		/// For unit tests.
		/// </summary>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="networkService">The network service for network access.</param>
		/// <param name="navigationService">The navigation service.</param>
		/// <param name="uiDispatcher"> The dispatcher to use for alerts and accessing the main thread.</param>
		protected internal MyThingsViewModel(INeuronService neuronService, INetworkService networkService, INavigationService navigationService, IUiDispatcher uiDispatcher)
		{
			this.neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
			this.networkService = networkService ?? DependencyService.Resolve<INetworkService>();
			this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
			this.uiDispatcher = uiDispatcher ?? DependencyService.Resolve<IUiDispatcher>();
			this.Things = new ObservableCollection<ContactInfo>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			SortedDictionary<string, ContactInfo> Sorted = new SortedDictionary<string, ContactInfo>();
			string Name;
			string Suffix;
			int i;

			foreach (ContactInfo Info in await Database.Find<ContactInfo>(new FilterFieldEqualTo("IsThing", true)))
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

			this.Things.Clear();
			foreach (ContactInfo Info in Sorted.Values)
				this.Things.Add(Info);

			this.ShowThingsMissing = Sorted.Count == 0;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.ShowThingsMissing = false;
			this.Things.Clear();
			await base.DoUnbind();
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
				if (b is MyThingsViewModel viewModel &&
					newValue is ContactInfo Thing)
				{
					viewModel.uiDispatcher.BeginInvokeOnMainThread(async () =>
					{
						await viewModel.navigationService.GoToAsync(nameof(ViewThingPage), new ViewThingNavigationArgs(Thing));
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