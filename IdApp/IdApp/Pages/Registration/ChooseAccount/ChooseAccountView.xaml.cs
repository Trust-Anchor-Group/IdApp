using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Registration.ChooseAccount
{
    /// <summary>
    /// A view to display the 'create account or connect to existing account' during the registration process.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChooseAccountView
    {
        private const int MaxAccountNameLength = 10;
        private const string SpecialCharacters = "\x20\x22\x26\x27\x2F\x3A\x3C\x3E\x40\xA0" +
            "\x1\x2\x3\x4\x5\x6\x7\x8\x9\xA\xB\xC\xD\xE\xF\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1A\x1B\x1C\x1D\x1E\x1F";

        /// <summary>
        /// Creates a new instance of the <see cref="ChooseAccountView"/> class.
        /// </summary>
        public ChooseAccountView()
        {
            InitializeComponent();
        }

        private void AccountNameTextChanged(object sender, TextChangedEventArgs e)
        {
            string NewTextValue = e.NewTextValue;

            if (!string.IsNullOrWhiteSpace(NewTextValue))
            {
                if (NewTextValue.Length > MaxAccountNameLength)
                {
                    NewTextValue = NewTextValue.Remove(MaxAccountNameLength);
                }

                for (int i = 0; i < NewTextValue.Length; i++)
                {
                    char x = NewTextValue[i];

                    if (SpecialCharacters.Contains(x))
                    {
                        NewTextValue = NewTextValue.Remove(i);
                    }
                }
            }

            ((Entry)sender).Text = NewTextValue;
        }
    }
}