using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using EDaler;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// The view model to bind to for when displaying a list of service providers.
	/// </summary>
	public class ServiceProvidersViewModel : XmppViewModel
	{
		private TaskCompletionSource<IBuyEDalerServiceProvider> selected;

		/// <summary>
		/// Creates an instance of the <see cref="ServiceProvidersViewModel"/> class.
		/// </summary>
		public ServiceProvidersViewModel()
			: base()
		{
			this.BackCommand = new Command(async _ => await this.GoBack());
			this.FromUserCommand = new Command(async _ => this.FromUser());

			this.ServiceProviders = new ObservableCollection<ServiceProviderModel>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out ServiceProvidersNavigationArgs args))
			{
				this.selected = args.Selected;
				this.Description = args.Description;

				foreach (IBuyEDalerServiceProvider ServiceProvider in args.ServiceProviders)
					this.ServiceProviders.Add(new ServiceProviderModel(ServiceProvider));
			}
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
			this.selected.TrySetResult(new EmptyServiceProvider());
		}

	}
}
