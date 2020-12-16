namespace XamarinApp.Extensions
{
    public static class BooleanExtensions
    {
        public static string ToYesNo(this bool b)
        {
            return b ? AppResources.Yes : AppResources.No;
        }
    }
}