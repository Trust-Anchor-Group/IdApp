using System;
using Xamarin.Forms;

namespace IdApp.Behaviors
{
    /// <summary>
    /// Used for unfocusing an input control.
    /// </summary>
    public class UnfocusOnClickedBehavior : Behavior<Button>
    {
        /// <summary>
        /// The view to unfocus.
        /// </summary>
        [TypeConverter(typeof(ReferenceTypeConverter))]
        public View UnfocusControl { get; set; }

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
            Unfocus(UnfocusControl);
        }

        /// <summary>
        /// Sets focus on an element.
        /// </summary>
        /// <param name="Element">Element to focus on.</param>
        public static void Unfocus(View Element)
		{
            if (!(Element is null) && Element.IsVisible)
                Element.Unfocus();
        }
    }
}