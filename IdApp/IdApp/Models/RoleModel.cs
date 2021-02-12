namespace IdApp.Models
{
    /// <summary>
    /// The data model for a contract role.
    /// </summary>
    public class RoleModel
    {
        /// <summary>
        /// Creates an instance of the <see cref="RoleModel"/> class.
        /// </summary>
        /// <param name="name">The name of the role.</param>
        public RoleModel(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// The name of the role.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns a string representation, i.e. name, of this <see cref="RoleModel"/> instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
}