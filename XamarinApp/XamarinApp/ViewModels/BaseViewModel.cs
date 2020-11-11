using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinApp.ViewModels
{
    public class BaseViewModel : BindableObject
    {
        public bool IsBound { get; private set; }

        public async Task Bind()
        {
            if (!IsBound)
            {
                await DoBind();
                IsBound = true;
            }
        }

        public async Task Unbind()
        {
            if (IsBound)
            {
                await DoUnbind();
                IsBound = false;
            }
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