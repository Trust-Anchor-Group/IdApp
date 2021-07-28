using System;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.Behaviors
{
    /// <summary>
    /// Used for moving focus to the next UI component when a button has been clicked.
    /// </summary>
    public class SetFocusOnClickedBehavior : Behavior<Button>
    {
        /// <summary>
        /// The view to move focus to.
        /// </summary>
        [TypeConverter(typeof(ReferenceTypeConverter))]
        public View SetFocusTo { get; set; }

        /// <inheritdoc/>
        protected override void OnAttachedTo(Button Button)
        {
			Button.Clicked += Button_Clicked;
            base.OnAttachedTo(Button);
        }

		/// <inheritdoc/>
		protected override void OnDetachingFrom(Button Button)
        {
            Button.Clicked -= Button_Clicked;
            base.OnDetachingFrom(Button);
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            View Element = SetFocusTo;
            if (!(Element is null) && Element.IsVisible)
                Element.Focus();
        }
    }
}