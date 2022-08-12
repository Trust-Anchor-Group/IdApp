using System.ComponentModel;
using IdApp.Helpers;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportEffect(typeof(IdApp.iOS.Effects.PasswordMaskTogglerEffect), IdApp.Constants.Effects.PasswordMaskTogglerEffect)]
namespace IdApp.iOS.Effects
{
	public class PasswordMaskTogglerEffect : PlatformEffect
	{
		private UITextField TextField => this.Control as UITextField;

		private Entry Entry => this.Element as Entry;

		protected override void OnAttached()
		{
			if (this.TextField != null)
			{
				this.TextField.EditingDidBegin += this.OnEditingBegin;
			}

			this.UpdateSecureTextEntry();
		}

		protected override void OnDetached()
		{
			if (this.TextField != null)
			{
				this.TextField.EditingDidBegin -= this.OnEditingBegin;
			}
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
			if (this.Entry != null && this.Entry.IsPassword && this.TextField != null)
			{
				bool IsPasswordMasked = PasswordMask.GetIsEnabled(this.Entry);
				if (IsPasswordMasked)
				{
					this.TextField.SecureTextEntry = true;
				}
				else
				{
					this.TextField.SecureTextEntry = false;
					this.TextField.AutocorrectionType = UITextAutocorrectionType.No;
					this.TextField.AutocapitalizationType = UITextAutocapitalizationType.None;
					this.TextField.SpellCheckingType = UITextSpellCheckingType.No;
				}
			}
		}

		private void OnEditingBegin(object Sender, System.EventArgs e)
		{
			if (this.TextField.SecureTextEntry && !string.IsNullOrEmpty(this.TextField.Text) && this.Entry != null)
			{
				string OriginalText = this.TextField.Text;

				// Disrupt the UITextField so that the password is cleared now but future edits will not clear it.
				this.TextField.DeleteBackward();

				// Restore the original value, having been cleared by the previous disruption.
				this.TextField.Text = OriginalText;

				// Just setting UITextField.Text property does not raise EditingChanged event, which the renderer monitors.
				// Calling UITextField.InsertText(string) does raise the event but leads to an ugly effect of the last character
				// being shown for a moment.
				((IElementController)this.Entry).SetValueFromRenderer(Entry.TextProperty, OriginalText);
			}
		}
	}
}
