using Xamarin.Forms;

namespace IdApp.Helpers
{
	public static class EntryProperties
	{
		public static readonly BindableProperty BorderColorProperty = BindableProperty.CreateAttached("BorderColor", typeof(Color), typeof(EntryProperties),
			defaultValue: Color.Transparent);

		public static Color GetBorderColor(BindableObject Bindable)
		{
			return (Color)Bindable.GetValue(BorderColorProperty);
		}

		public static void SetBorderColor(BindableObject Bindable, Color Color)
		{
			Bindable.SetValue(BorderColorProperty, Color);
		}
	}
}
