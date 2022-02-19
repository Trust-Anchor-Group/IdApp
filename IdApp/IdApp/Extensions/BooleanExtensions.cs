using IdApp.Resx;

namespace IdApp.Extensions
{
    /// <summary>
    /// An extension class for the boolean primitive type.
    /// </summary>
    public static class BooleanExtensions
    {
        /// <summary>
        /// Converts a boolean value to the localized string 'Yes' if <c>true</c>, or 'No' if <c>false</c>.
        /// </summary>
        /// <param name="b"></param>
        /// <returns>String representation</returns>
        public static string ToYesNo(this bool b)
        {
            return b ? AppResources.Yes : AppResources.No;
        }
    }
}