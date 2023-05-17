using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using IServiceProvider = Waher.Networking.XMPP.Contracts.IServiceProvider;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// The view model to bind to for when displaying a list of service providers.
	/// </summary>
	public class ServiceProvidersViewModel : XmppViewModel
	{
		private bool useShellNavigationService;
		private ServiceProvidersNavigationArgs navigationArgs;

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
		/// <param name="NavigationArgs">Navigation arguments.</param>
		public ServiceProvidersViewModel(ServiceProvidersNavigationArgs NavigationArgs)
			: base()
		{
			this.useShellNavigationService = NavigationArgs is null;
			this.navigationArgs = NavigationArgs;
			this.BackCommand = new Command(_ => this.GoBack());
			this.ServiceProviders = new ObservableCollection<ServiceProviderModel>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.navigationArgs is null && this.NavigationService.TryPopArgs(out ServiceProvidersNavigationArgs Args))
			{
				this.navigationArgs = Args;
			}

			this.Title = this.navigationArgs?.Title;
			this.Description = this.navigationArgs?.Description;

			foreach (IServiceProvider ServiceProvider in this.navigationArgs?.ServiceProviders)
				this.ServiceProviders.Add(new ServiceProviderModel(ServiceProvider));
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			if (this.navigationArgs?.ServiceProvider is TaskCompletionSource<IServiceProvider> TaskSource)
			{
				TaskSource.TrySetResult(null);
			}

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// See <see cref="Title"/>
		/// </summary>
		public static readonly BindableProperty TitleProperty =
			BindableProperty.Create(nameof(Title), typeof(string), typeof(ServiceProvidersViewModel), default(string));

		/// <summary>
		/// Title to show the user.
		/// </summary>
		public string Title
		{
			get => (string)this.GetValue(TitleProperty);
			set => this.SetValue(TitleProperty, value);
		}

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
			this.TrySetResultAndClosePage(ServiceProvider.ServiceProvider);
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

		#endregion

		private void GoBack()
		{
			this.TrySetResultAndClosePage(null);
		}

		private void TrySetResultAndClosePage(IServiceProvider ServiceProvider)
		{
			TaskCompletionSource<IServiceProvider> TaskSource = this.navigationArgs.ServiceProvider;
			this.navigationArgs.ServiceProvider = null;

			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					if (this.useShellNavigationService)
						await this.NavigationService.GoBackAsync();
					else
						await App.Current.MainPage.Navigation.PopAsync();

					if (TaskSource is not null)
					{
						TaskSource.TrySetResult(ServiceProvider);
					}
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			});
		}
	}
}
