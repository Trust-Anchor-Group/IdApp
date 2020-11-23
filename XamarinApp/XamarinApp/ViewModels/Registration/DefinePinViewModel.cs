using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class DefinePinViewModel : RegistrationStepViewModel
    {
        public DefinePinViewModel(RegistrationStep step, TagProfile tagProfile, INeuronService neuronService, IMessageService messageService)
            : base(step, tagProfile, neuronService, messageService)
        {
        }

        public static readonly BindableProperty PinProperty =
            BindableProperty.Create("Pin", typeof(string), typeof(DefinePinViewModel), default(string));

        public string Pin
        {
            get { return (string)GetValue(PinProperty); }
            set { SetValue(PinProperty, value); }
        }

        public static readonly BindableProperty RetypePinProperty =
            BindableProperty.Create("RetypePin", typeof(string), typeof(DefinePinViewModel), default(string));

        public string RetypePin
        {
            get { return (string)GetValue(RetypePinProperty); }
            set { SetValue(RetypePinProperty, value); }
        }

        public static readonly BindableProperty UsePinProperty =
            BindableProperty.Create("UsePin", typeof(bool), typeof(DefinePinViewModel), default(bool));

        public bool UsePin
        {
            get { return (bool)GetValue(UsePinProperty); }
            set { SetValue(UsePinProperty, value); }
        }
    }
}