namespace XamarinApp.Models
{
    public class RoleModel
    {
        public RoleModel(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}