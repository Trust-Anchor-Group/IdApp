namespace XamarinApp.Models
{
    public class ParameterModel
    {
        public ParameterModel(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}