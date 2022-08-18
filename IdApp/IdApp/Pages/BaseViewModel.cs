using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Extensions;
using IdApp.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages
{
	/// <summary>
	/// A base class for all view models, inheriting from the <see cref="BindableObject"/>.
	/// <br/>
	/// NOTE: using this class requires your page/view to inherit from <see cref="ContentBasePage"/>.
	/// </summary>
	public abstract class BaseViewModel : ServiceReferences, ILifeCycleView
	{
		private readonly List<BaseViewModel> childViewModels;

		/// <summary>
		/// Create an instance of a <see cref="BaseViewModel"/>.
		/// </summary>
		public BaseViewModel()
		{
			this.childViewModels = new List<BaseViewModel>();
		}

		/// <summary>
		/// Returns <c>true</c> if the viewmodel is bound, i.e. its parent page has appeared on screen.
		/// </summary>
		public bool IsAppearing { get; private set; }

		/// <summary>
		/// Called by the parent page when it appears on screen.
		/// </summary>
		public async Task Appearing()
		{
			if (!this.IsAppearing)
			{
				DeviceDisplay.KeepScreenOn = true;

				await this.OnAppearing();

				foreach (BaseViewModel childViewModel in this.childViewModels)
					await childViewModel.Appearing();

				this.IsAppearing = true;
			}
		}

		/// <summary>
		/// Called by the parent page when it disappears from screen.
		/// </summary>
		public async Task Disappearing()
		{
			if (this.IsAppearing)
			{
				foreach (BaseViewModel childViewModel in this.childViewModels)
					await childViewModel.Disappearing();

				await this.OnDisappearing();
				this.IsAppearing = false;
			}
		}

		/// <summary>
		/// Gets the child view models.
		/// </summary>
		public IEnumerable<BaseViewModel> Children => this.childViewModels;

		/// <summary>
		/// Use this method when nesting view models. This is the viewmodel equivalent of master/detail pages.
		/// </summary>
		/// <typeparam name="T">The viewmodel type.</typeparam>
		/// <param name="childViewModel">The child viewmodel to add.</param>
		/// <returns>Child view model</returns>
		protected T AddChildViewModel<T>(T childViewModel) where T : BaseViewModel
		{
			this.childViewModels.Add(childViewModel);
			return childViewModel;
		}

		/// <summary>
		/// Use this method when nesting view models. This is the viewmodel equivalent of master/detail pages.
		/// </summary>
		/// <typeparam name="T">The viewmodel type.</typeparam>
		/// <param name="childViewModel">The child viewmodel to remove.</param>
		/// <returns>Child view model</returns>
		protected T RemoveChildViewModel<T>(T childViewModel) where T : BaseViewModel
		{
			this.childViewModels.Remove(childViewModel);
			return childViewModel;
		}

		/// <summary>
		/// Called by the parent page when it appears on screen, <em>after</em> the <see cref="Appearing"/> method is called.
		/// </summary>
		public async Task RestoreState()
		{
			foreach (BaseViewModel childViewModel in this.childViewModels)
				await childViewModel.DoRestoreState();

			await this.DoRestoreState();
		}

		/// <summary>
		/// Called by the parent page when it disappears on screen, <em>before</em> the <see cref="Disappearing"/> method is called.
		/// </summary>
		public async Task SaveState()
		{
			foreach (BaseViewModel childViewModel in this.childViewModels)
				await childViewModel.DoSaveState();

			await this.DoSaveState();
		}

		/// <summary>
		/// Convenience method that calls <see cref="SaveState"/> and then <see cref="Disappearing"/>.
		/// </summary>
		public async Task Shutdown()
		{
			await this.SaveState();
			await this.Disappearing();
		}

		/// <summary>
		/// Override this method to do view model specific restoring of state when it's parent page/view appears on screen.
		/// </summary>
		protected virtual Task DoRestoreState()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Override this method to do view model specific saving of state when it's parent page/view disappears from screen.
		/// </summary>
		protected virtual Task DoSaveState()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Helper method for getting a unique settings key for a given property.
		/// </summary>
		/// <param name="propertyName">The property name to convert into a settings key.</param>
		/// <returns>Key name</returns>
		protected string GetSettingsKey(string propertyName)
		{
			return this.GetType().FullName + "." + propertyName;
		}

		/// <summary>
		/// Executes the <see cref="Command.ChangeCanExecute"/> method on all commands.
		/// This is done to update the UI state for those UI components that bind to a command.
		/// </summary>
		/// <param name="commands">The commands to evaluate.</param>
		protected void EvaluateCommands(params ICommand[] commands)
		{
			foreach (ICommand command in commands)
				command.ChangeCanExecute();
		}

		/// <summary>
		/// <see cref="IsBusy"/>
		/// </summary>
		public static readonly BindableProperty IsBusyProperty =
			BindableProperty.Create(nameof(IsBusy), typeof(bool), typeof(BaseViewModel), default(bool));

		/// <summary>
		/// A helper property to set/get when the ViewModel is busy doing work.
		/// </summary>
		public bool IsBusy
		{
			get => (bool)this.GetValue(IsBusyProperty);
			set
			{
				this.SetValue(IsBusyProperty, value);
				this.SetValue(IsIdleProperty, !value);
			}
		}

		/// <summary>
		/// <see cref="IsIdle"/>
		/// </summary>
		public static readonly BindableProperty IsIdleProperty =
			BindableProperty.Create(nameof(IsIdle), typeof(bool), typeof(BaseViewModel), default(bool));

		/// <summary>
		/// A helper property to set/get when the ViewModel is idle.
		/// </summary>
		public bool IsIdle
		{
			get => (bool)this.GetValue(IsIdleProperty);
			set
			{
				this.SetValue(IsIdleProperty, value);
				this.SetValue(IsBusyProperty, !value);
			}
		}

		/// <summary>
		/// A helper method for synchronously setting this registration step to Done, and also calling
		/// <see cref="Command.ChangeCanExecute"/> on the list of commands passed in.
		/// </summary>
		/// <param name="commands">The commands to re-evaluate.</param>
		protected void SetIsDone(params ICommand[] commands)
		{
			this.IsBusy = false;
			this.EvaluateCommands(commands);
		}

		/// <summary>
		/// Sets the <see cref="BaseViewModel.IsBusy"/> flag for this instance. Also calls
		/// <see cref="Command.ChangeCanExecute"/> on the list of commands passed in.
		/// </summary>
		/// <param name="commands">The commands to re-evaluate.</param>
		protected void SetIsBusy(params ICommand[] commands)
		{
			this.IsBusy = true;
			this.EvaluateCommands(commands);
		}

		/// <summary>
		/// Method called when view is initialized for the first time. Use this method to implement registration
		/// of event handlers, processing navigation arguments, etc.
		/// </summary>
		public virtual Task OnInitialize()
		{
			return Task.CompletedTask;  // Do nothing by default.
		}

		/// <summary>
		/// Method called when the view is disposed, and will not be used more. Use this method to unregister
		/// event handlers, etc.
		/// </summary>
		public virtual Task OnDispose()
		{
			return Task.CompletedTask;  // Do nothing by default.
		}

		/// <summary>
		/// Method called when view is appearing on the screen.
		/// </summary>
		public virtual Task OnAppearing()
		{
			return Task.CompletedTask;  // Do nothing by default.
		}

		/// <summary>
		/// Method called when view is disappearing from the screen.
		/// </summary>
		public virtual Task OnDisappearing()
		{
			return Task.CompletedTask;  // Do nothing by default.
		}
	}
}
