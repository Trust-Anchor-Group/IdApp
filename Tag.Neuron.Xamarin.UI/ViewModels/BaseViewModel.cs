using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.ViewModels
{
    /// <summary>
    /// A base class for all view models, inheriting from the <see cref="BindableObject"/>.
    /// This class provides default implementations for the <see cref="DoBind"/> and <see cref="DoUnbind"/> methods.
    /// Override those to implement setup/teardown when a page/view is appearing and disapperaring to/from the screen respectively.
    /// </summary>
    public class BaseViewModel : BindableObject
    {
        private readonly List<BaseViewModel> childViewModels;

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
                await DoBind();
                foreach (BaseViewModel childViewModel in childViewModels)
                {
                    await childViewModel.DoBind();
                }
                IsBound = true;
            }
        }

        /// <summary>
        /// Called by the parent page when it disaappears from screen.
        /// </summary>
        /// <returns></returns>
        public async Task Unbind()
        {
            if (IsBound)
            {
                foreach (BaseViewModel childViewModel in childViewModels)
                {
                    await childViewModel.DoUnbind();
                }
                await DoUnbind();
                IsBound = false;
            }
        }

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

        public async Task RestoreState()
        {
            foreach (BaseViewModel childViewModel in childViewModels)
            {
                await childViewModel.DoRestoreState();
            }
            await DoRestoreState();
        }

        public async Task SaveState()
        {
            foreach (BaseViewModel childViewModel in childViewModels)
            {
                await childViewModel.DoSaveState();
            }
            await DoSaveState();
        }

        protected virtual Task DoRestoreState()
        {
            return Task.CompletedTask;
        }

        protected virtual Task DoSaveState()
        {
            return Task.CompletedTask;
        }

        protected string GetSettingsKey(string propertyName)
        {
            return $"{this.GetType().FullName}.{propertyName}";
        }

        public static readonly BindableProperty IsBusyProperty =
            BindableProperty.Create("IsBusy", typeof(bool), typeof(BaseViewModel), default(bool));

        public bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }
    }
}