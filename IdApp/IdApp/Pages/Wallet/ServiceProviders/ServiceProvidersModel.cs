using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// The view model to bind to for when displaying a list of service providers.
	/// </summary>
	public class ServiceProvidersViewModel : XmppViewModel
	{
		private TaskCompletionSource<IServiceProvider> selected;
		private ServiceProvidersNavigationArgs presetArgs;

		/// <summary>
		/// Creates an instance of the <see cref="ServiceProvidersViewModel"/> class.
		/// </summary>
		public ServiceProvidersViewModel()
			: this(null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ServiceProvidersViewModel"/> class.
		/// </summary>
		/// <param name="e">Navigation arguments.</param>
		public ServiceProvidersViewModel(ServiceProvidersNavigationArgs e)
			: base()
		{
			this.presetArgs = e;

			this.BackCommand = new Command(async _ => await this.GoBack());
			this.FromUserCommand = new Command(_ => this.FromUser());

			this.ServiceProviders = new ObservableCollection<ServiceProviderModel>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			ServiceProvidersNavigationArgs args = this.presetArgs;
			if (args is null)
			{
				if (!this.NavigationService.TryPopArgs(out args))
					return;
			}

			this.selected = args.Selected;
			this.Description = args.Description;

			foreach (IServiceProvider ServiceProvider in args.ServiceProviders)
				this.ServiceProviders.Add(new ServiceProviderModel(ServiceProvider));
		}

		/// <inheritdoc/>
		protected override async Task OnAppearing()
		{
			await base.OnAppearing();

			if (this.selected is not null && this.selected.Task.IsCompleted)
			{
				await this.NavigationService.GoBackAsync();
				return;
			}

			this.SelectedServiceProvider = null;
		}

		/// <inheritdoc/>
		protected override Task OnDispose()
		{
			this.selected?.TrySetResult(null);

			return base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// See <see cref="Description"/>
		/// </summary>
		public static readonly BindableProperty DescriptionProperty =
			BindableProperty.Create(nameof(Description), typeof(string), typeof(ServiceProvidersViewModel), default(string));

		/// <summary>
		/// Description to show the user.
		/// </summary>
		public string Description
		{
			get => (string)this.GetValue(DescriptionProperty);
			set => this.SetValue(DescriptionProperty, value);
		}

		/// <summary>
		/// Holds a list of service providers
		/// </summary>
		public ObservableCollection<ServiceProviderModel> ServiceProviders { get; }

		/// <summary>
		/// See <see cref="SelectedServiceProvider"/>
		/// </summary>
		public static readonly BindableProperty SelectedServiceProviderProperty =
			BindableProperty.Create(nameof(SelectedServiceProvider), typeof(ServiceProviderModel), typeof(ServiceProvidersViewModel), default(ServiceProviderModel),
				propertyChanged: (b, oldValue, newValue) =>
				{
					if (b is ServiceProvidersViewModel viewModel && newValue is ServiceProviderModel ServiceProvider)
						viewModel.OnSelected(ServiceProvider);
				});

		private void OnSelected(ServiceProviderModel ServiceProvider)
		{
			this.selected?.TrySetResult(ServiceProvider.ServiceProvider);
		}

		/// <summary>
		/// The currently selected contact, if any.
		/// </summary>
		public ServiceProviderModel SelectedServiceProvider
		{
			get => (ServiceProviderModel)this.GetValue(SelectedServiceProviderProperty);
			set => this.SetValue(SelectedServiceProviderProperty, value);
		}

		/// <summary>
		/// The command to bind to for returning to previous view.
		/// </summary>
		public ICommand BackCommand { get; }

		/// <summary>
		/// The command to bind to for generating a code for another user.
		/// </summary>
		public ICommand FromUserCommand { get; }

		#endregion

		private async Task GoBack()
		{
			await this.NavigationService.GoBackAsync();
			this.selected.TrySetResult(null);
		}

		private void FromUser()
		{
			this.selected.TrySetResult(new EmptyBuyEDalerServiceProvider());
		}

	}
}
