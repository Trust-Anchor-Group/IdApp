using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin.UI.Extensions;
using Tag.Neuron.Xamarin.UI.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.ViewModels
{
    /// <summary>
    /// A base class for all view models, inheriting from the <see cref="BindableObject"/>.
    /// This class provides default implementations for the <see cref="DoBind"/> and <see cref="DoUnbind"/> methods.
    /// Override those to implement setup/teardown when a page/view is appearing and disappearing to/from the screen respectively.
    /// <br/>
    /// NOTE: using this class requires your page/view to inherit from <see cref="ContentBasePage"/>.
    /// </summary>
    public class BaseViewModel : BindableObject
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
        public bool IsBound { get; private set; }

        /// <summary>
        /// Called by the parent page when it appears on screen.
        /// </summary>
        /// <returns></returns>
        public async Task Bind()
        {
            if (!IsBound)
            {
                DeviceDisplay.KeepScreenOn = true;

                await DoBind();
                
                foreach (BaseViewModel childViewModel in childViewModels)
                    await childViewModel.Bind();
                
                IsBound = true;
            }
        }

        /// <summary>
        /// Called by the parent page when it disappears from screen.
        /// </summary>
        /// <returns></returns>
        public async Task Unbind()
        {
            if (IsBound)
            {
                foreach (BaseViewModel childViewModel in childViewModels)
                {
                    await childViewModel.Unbind();
                }
                await DoUnbind();
                IsBound = false;
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
        /// <returns></returns>
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
        /// <returns></returns>
        protected T RemoveChildViewModel<T>(T childViewModel) where T : BaseViewModel
        {
            this.childViewModels.Remove(childViewModel);
            return childViewModel;
        }

        /// <summary>
        /// Override this method to do view model specific setup when it's parent page/view appears on screen.
        /// </summary>
        /// <returns></returns>
        protected virtual Task DoBind()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this method to do view model specific teardown when it's parent page/view disappears from screen.
        /// </summary>
        /// <returns></returns>
        protected virtual Task DoUnbind()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called by the parent page when it appears on screen, <em>after</em> the <see cref="Bind"/> method is called.
        /// </summary>
        /// <returns></returns>
        public async Task RestoreState()
        {
            foreach (BaseViewModel childViewModel in childViewModels)
            {
                await childViewModel.DoRestoreState();
            }
            await DoRestoreState();
        }

        /// <summary>
        /// Called by the parent page when it disappears on screen, <em>before</em> the <see cref="Unbind"/> method is called.
        /// </summary>
        /// <returns></returns>
        public async Task SaveState()
        {
            foreach (BaseViewModel childViewModel in childViewModels)
            {
                await childViewModel.DoSaveState();
            }
            await DoSaveState();
        }

        /// <summary>
        /// Convenience method that calls <see cref="SaveState"/> and then <see cref="Unbind"/>.
        /// </summary>
        /// <returns></returns>
        public async Task Shutdown()
        {
            await this.SaveState();
            await this.Unbind();
        }

        /// <summary>
        /// Override this method to do view model specific restoring of state when it's parent page/view appears on screen.
        /// </summary>
        /// <returns></returns>
        protected virtual Task DoRestoreState()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this method to do view model specific saving of state when it's parent page/view disappears from screen.
        /// </summary>
        /// <returns></returns>
        protected virtual Task DoSaveState()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Helper method for getting a unique settings key for a given property.
        /// </summary>
        /// <param name="propertyName">The property name to convert into a settings key.</param>
        /// <returns></returns>
        protected string GetSettingsKey(string propertyName)
        {
            return $"{this.GetType().FullName}.{propertyName}";
        }

        /// <summary>
        /// Executes the <see cref="Command.ChangeCanExecute"/> method on all commands.
        /// This is done to update the UI state for those UI components that bind to a command.
        /// </summary>
        /// <param name="commands">The commands to evaluate.</param>
        protected void EvaluateCommands(params ICommand[] commands)
        {
            foreach (ICommand command in commands)
            {
                command.ChangeCanExecute();
            }
        }

        /// <summary>
        /// <see cref="IsBusy"/>
        /// </summary>
        public static readonly BindableProperty IsBusyProperty =
            BindableProperty.Create("IsBusy", typeof(bool), typeof(BaseViewModel), default(bool));

        /// <summary>
        /// A helper property to set/get when the ViewModel is busy doing work.
        /// </summary>
        public bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }
    }
}