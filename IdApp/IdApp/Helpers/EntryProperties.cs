using Xamarin.Forms;

namespace IdApp.Helpers
{
	/// <summary>
	/// EntryProperties is a class which defines attached bindable properties used by our custom renderers for <see cref="Entry"/>.
	/// </summary>
	public class EntryProperties
	{
		/// <summary>
		/// Implements the attached property that defines the color of the border around an <see cref="Entry"/>.
		/// </summary>
		public static readonly BindableProperty BorderColorProperty
			= BindableProperty.CreateAttached("BorderColor", typeof(Color), typeof(EntryProperties), Color.Default);

		/// <summary>
		/// Gets the color of the border around an <see cref="Entry"/>.
		/// </summary>
		public static Color GetBorderColor(BindableObject Bindable)
		{
			return (Color)Bindable.GetValue(BorderColorProperty);
		}

		/// <summary>
		/// Sets the color of the border around an <see cref="Entry"/>.
		/// </summary>
		public static void SetBorderColor(BindableObject Bindable, Color Value)
		{
			Bindable.SetValue(BorderColorProperty, Value);
		}
	}
}
