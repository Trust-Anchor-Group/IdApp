using Xamarin.Forms;

namespace IdApp.Pages.Contracts.ViewContract.ObjectModel
{
    /// <summary>
    /// The data model for a contract part.
    /// </summary>
    public class PartModel : BindableObject
    {
        /// <summary>
        /// Creates an instance of the <see cref="PartModel"/> class.
        /// </summary>
        /// <param name="key">A unique contract part key.</param>
        /// <param name="value">The contract part value.</param>
        /// <param name="legalId">A legal id (optional).</param>
        public PartModel(string key, string value, string legalId = null)
        {
            this.Key = key;
            this.Value = value;
            this.LegalId = legalId;
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty KeyProperty =
            BindableProperty.Create(nameof(Key), typeof(string), typeof(PartModel), default(string));

        /// <summary>
        /// A unique contract part key.
        /// </summary>
        public string Key
        {
            get { return (string)this.GetValue(KeyProperty); }
            set { this.SetValue(KeyProperty, value); }
        }

        /// <summary>
        /// The contract part value.
        /// </summary>
        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(string), typeof(PartModel), default(string));

        /// <summary>
        /// 
        /// </summary>
        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty LegalIdProperty =
            BindableProperty.Create(nameof(LegalId), typeof(string), typeof(PartModel), default(string));

        /// <summary>
        /// A legal id (optional).
        /// </summary>
        public string LegalId
        {
            get { return (string)this.GetValue(LegalIdProperty); }
            set { this.SetValue(LegalIdProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty CanSignProperty =
            BindableProperty.Create(nameof(CanSign), typeof(bool), typeof(PartModel), default(bool));

        /// <summary>
        /// Gets or sets whether the contract part can sign a contract.
        /// </summary>
        public bool CanSign
        {
            get { return (bool)this.GetValue(CanSignProperty); }
            set { this.SetValue(CanSignProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty SignAsRoleProperty =
            BindableProperty.Create(nameof(SignAsRole), typeof(string), typeof(PartModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                PartModel model = (PartModel)b;
                model.CanSign = !string.IsNullOrWhiteSpace((string)newValue);
            });

        /// <summary>
        /// The role to use when signing.
        /// </summary>
        public string SignAsRole
        {
            get { return (string)this.GetValue(SignAsRoleProperty); }
            set { this.SetValue(SignAsRoleProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty SignAsRoleTextProperty =
            BindableProperty.Create(nameof(SignAsRoleText), typeof(string), typeof(PartModel), default(string));

        /// <summary>
        /// The free text value of the 'sign as role'
        /// </summary>
        public string SignAsRoleText
        {
            get { return (string)this.GetValue(SignAsRoleTextProperty); }
            set { this.SetValue(SignAsRoleTextProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty IsHtmlProperty =
            BindableProperty.Create(nameof(IsHtml), typeof(bool), typeof(PartModel), default(bool));

        /// <summary>
        /// Gets or sets whether the format of the contract part is html or not.
        /// </summary>
        public bool IsHtml
        {
            get { return (bool)this.GetValue(IsHtmlProperty); }
            set { this.SetValue(IsHtmlProperty, value); }
        }
    }
}