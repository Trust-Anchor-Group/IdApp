using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinApp.ViewModels
{
    public class BaseViewModel : BindableObject
    {
        public Task Bind()
        {
            return DoBind();
        }

        public Task Unbind()
        {
            return DoUnbind();
        }

        protected virtual Task DoBind()
        {
            return Task.CompletedTask;
        }

        protected virtual Task DoUnbind()
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