using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinApp.ViewModels
{
    public class BaseViewModel : BindableObject
    {
        private readonly List<BaseViewModel> childViewModels;

        public BaseViewModel()
        {
            this.childViewModels = new List<BaseViewModel>();
        }

        public bool IsBound { get; private set; }

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

        protected T AddChildViewModel<T>(T childViewModel) where T : BaseViewModel
        {
            this.childViewModels.Add(childViewModel);
            return childViewModel;
        }

        protected virtual Task DoBind()
        {
            return Task.CompletedTask;
        }

        protected virtual Task DoUnbind()
        {
            return Task.CompletedTask;
        }

        public virtual Task RestoreState()
        {
            return Task.CompletedTask;
        }

        public virtual Task SaveState()
        {
            return Task.CompletedTask;
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