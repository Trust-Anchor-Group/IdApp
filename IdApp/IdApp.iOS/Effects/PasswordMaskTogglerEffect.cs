using System.ComponentModel;
using IdApp.Helpers;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ResolutionGroupName(IdApp.Constants.Effects.ResolutionGroupName)]
[assembly: ExportEffect(typeof(IdApp.iOS.Effects.PasswordMaskTogglerEffect), IdApp.Constants.Effects.PasswordMaskTogglerEffect)]

namespace IdApp.iOS.Effects
{
	internal class PasswordMaskTogglerEffect : PlatformEffect
	{
		protected override void OnAttached()
		{
			this.UpdateSecureTextEntry();
		}

		protected override void OnDetached()
		{
		}

		protected override void OnElementPropertyChanged(PropertyChangedEventArgs args)
		{
			base.OnElementPropertyChanged(args);

			if (args.PropertyName == PasswordMask.IsEnabledProperty.PropertyName || args.PropertyName == Entry.IsPasswordProperty.PropertyName)
			{
				this.UpdateSecureTextEntry();
			}
		}

		private void UpdateSecureTextEntry()
		{
			if (this.Element is Entry Entry && Entry.IsPassword && this.Control is UITextField TextField)
			{
				bool IsPasswordMasked = PasswordMask.GetIsEnabled(Entry);
				if (IsPasswordMasked)
				{
					TextField.SecureTextEntry = true;
					TextField.EditingDidBegin += this.OnEditingBegin;
				}
				else
				{
					TextField.SecureTextEntry = false;
					TextField.AutocorrectionType = UITextAutocorrectionType.No;
					TextField.AutocapitalizationType = UITextAutocapitalizationType.None;
				}
			}
		}

		private void OnEditingBegin(object sender, System.EventArgs e)
		{
			UITextField TextField = (UITextField)this.Control;
			TextField.EditingDidBegin -= this.OnEditingBegin;
			if (!string.IsNullOrEmpty(TextField.Text))
			{
				string OriginalText = TextField.Text;

				// Disrupt the UITextField so that the password is cleared now but future edits do not clear the password.
				TextField.DeleteBackward();

				// Restore the original value, having been cleared by the previous disruption.
				TextField.Text = OriginalText;

				// Just setting UITextField.Text property does not raise EditingChanged event, which the renderer monitors.
				// Calling UITextField.InsertText(string) does raise the event but leads to an ugly effect of the last character
				// being shown for a moment.
				((IElementController)this.Element).SetValueFromRenderer(Entry.TextProperty, OriginalText);
			}
		}
	}
}
