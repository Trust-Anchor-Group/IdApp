using Xamarin.Forms;

namespace IdApp.Models
{
    public class ParameterModel : BindableObject
    {
        public ParameterModel(string id, string name)
        {
            this.Id = id;
            this.Name = name;
            this.IsValid = true;
        }

        public string Id { get; }

        public static readonly BindableProperty NameProperty =
            BindableProperty.Create("Name", typeof(string), typeof(ParameterModel), default(string));

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        public static readonly BindableProperty IsValidProperty =
            BindableProperty.Create("IsValid", typeof(bool), typeof(ParameterModel), default(bool));

        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            set { SetValue(IsValidProperty, value); }
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}