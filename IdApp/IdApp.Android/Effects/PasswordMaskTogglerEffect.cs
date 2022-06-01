using System.ComponentModel;
using Android.Text;
using Android.Widget;
using IdApp.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportEffect(typeof(IdApp.Android.Effects.PasswordMaskTogglerEffect), IdApp.Constants.Effects.PasswordMaskTogglerEffect)]
namespace IdApp.Android.Effects
{
	public class PasswordMaskTogglerEffect : PlatformEffect
	{
		protected override void OnAttached()
		{
			this.UpdateInputType();
		}

		protected override void OnDetached()
		{
		}

		protected override void OnElementPropertyChanged(PropertyChangedEventArgs args)
		{
			base.OnElementPropertyChanged(args);

			if (args.PropertyName == PasswordMask.IsEnabledProperty.PropertyName || args.PropertyName == Entry.IsPasswordProperty.PropertyName)
			{
				this.UpdateInputType();
			}
		}

		private void UpdateInputType()
		{
			if (this.Element is Entry Entry && Entry.IsPassword && this.Control is EditText EditText)
			{
				int SelectionStart = EditText.SelectionStart;
				int SelectionEnd = EditText.SelectionEnd;

				bool IsPasswordMasked = PasswordMask.GetIsEnabled(Entry);
				if (IsPasswordMasked)
				{
					EditText.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
				}
				else
				{
					EditText.InputType = InputTypes.ClassText | InputTypes.TextVariationVisiblePassword;
				}

				EditText.SetSelection(SelectionStart, SelectionEnd);

				// Setting input type to any password variation (masked or not) will reset the font typeface to the Android default value,
				// which differs from what was set by Xamarin renderer. The font size changes below will trigger the EditText renderer and
				// restore the Xamarin typeface.
				Entry.FontSize += 1;
				Entry.FontSize -= 1;
			}
		}
	}
}
