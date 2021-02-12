using Xamarin.Forms;

namespace IdApp.Models
{
    /// <summary>
    /// The data model for contract parameters.
    /// </summary>
    public class ParameterModel : BindableObject
    {
        /// <summary>
        /// Creates an instance of the <see cref="ParameterModel"/> class.
        /// </summary>
        /// <param name="id">The parameter id.</param>
        /// <param name="name">The parameter name.</param>
        public ParameterModel(string id, string name)
        {
            this.Id = id;
            this.Name = name;
            this.IsValid = true;
        }

        /// <summary>
        /// The parameter id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty NameProperty =
            BindableProperty.Create("Name", typeof(string), typeof(ParameterModel), default(string));

        /// <summary>
        /// The parameter name.
        /// </summary>
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty IsValidProperty =
            BindableProperty.Create("IsValid", typeof(bool), typeof(ParameterModel), default(bool));

        /// <summary>
        /// Gets or sets whether this parameter is valid or not.
        /// </summary>
        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            set { SetValue(IsValidProperty, value); }
        }

        /// <summary>
        /// Returns the string representation, i.e. name, of this <see cref="ParameterModel"/> instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}