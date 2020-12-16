using Xamarin.Forms;

namespace XamarinApp.Models
{
    public class PartModel : BindableObject
    {
        public PartModel(string key, string value, string legalId = null)
        {
            this.Key = key;
            this.Value = value;
            this.LegalId = legalId;
        }

        public static readonly BindableProperty KeyProperty =
            BindableProperty.Create("Key", typeof(string), typeof(PartModel), default(string));

        public string Key
        {
            get { return (string)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create("Value", typeof(string), typeof(PartModel), default(string));

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly BindableProperty LegalIdProperty =
            BindableProperty.Create("LegalId", typeof(string), typeof(PartModel), default(string));

        public string LegalId
        {
            get { return (string)GetValue(LegalIdProperty); }
            set { SetValue(LegalIdProperty, value); }
        }

        public static readonly BindableProperty CanSignProperty =
            BindableProperty.Create("CanSign", typeof(bool), typeof(PartModel), default(bool));

        public bool CanSign
        {
            get { return (bool)GetValue(CanSignProperty); }
            set { SetValue(CanSignProperty, value); }
        }

        public static readonly BindableProperty SignAsRoleProperty =
            BindableProperty.Create("SignAsRole", typeof(string), typeof(PartModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                PartModel model = (PartModel)b;
                model.CanSign = !string.IsNullOrWhiteSpace((string)newValue);
            });

        public string SignAsRole
        {
            get { return (string)GetValue(SignAsRoleProperty); }
            set { SetValue(SignAsRoleProperty, value); }
        }

        public static readonly BindableProperty SignAsRoleTextProperty =
            BindableProperty.Create("SignAsRoleText", typeof(string), typeof(PartModel), default(string));

        public string SignAsRoleText
        {
            get { return (string) GetValue(SignAsRoleTextProperty); }
            set { SetValue(SignAsRoleTextProperty, value); }
        }

        public static readonly BindableProperty IsHtmlProperty =
            BindableProperty.Create("IsHtml", typeof(bool), typeof(PartModel), default(bool));

        public bool IsHtml
        {
            get { return (bool) GetValue(IsHtmlProperty); }
            set { SetValue(IsHtmlProperty, value); }
        }
    }
}