﻿using Xamarin.Forms;

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

		/// <summary>
		/// Implements the attached property that defines the width of the border around an <see cref="Entry"/>.
		/// </summary>
		public static readonly BindableProperty BorderWidthProperty
			= BindableProperty.CreateAttached("BorderWidth", typeof(double), typeof(EntryProperties), 1);

		/// <summary>
		/// Gets the width of the border around an <see cref="Entry"/>.
		/// </summary>
		public static double GetBorderWidth(BindableObject Bindable)
		{
			return (double)Bindable.GetValue(BorderWidthProperty);
		}

		/// <summary>
		/// Sets the width of the border around an <see cref="Entry"/>.
		/// </summary>
		public static void SetBorderWidth(BindableObject Bindable, double Value)
		{
			Bindable.SetValue(BorderWidthProperty, Value);
		}

		/// <summary>
		/// Implements the attached property that defines the corner radius of an <see cref="Entry"/>.
		/// </summary>
		public static readonly BindableProperty CornerRadiusProperty
			= BindableProperty.CreateAttached("CornerRadius", typeof(double), typeof(EntryProperties), 5);

		/// <summary>
		/// Gets the corner radius of an <see cref="Entry"/>.
		/// </summary>
		public static double GetCornerRadius(BindableObject Bindable)
		{
			return (double)Bindable.GetValue(CornerRadiusProperty);
		}

		/// <summary>
		/// Sets the corner radius of an <see cref="Entry"/>.
		/// </summary>
		public static void SetCornerRadius(BindableObject Bindable, double Value)
		{
			Bindable.SetValue(CornerRadiusProperty, Value);
		}
	}
}
