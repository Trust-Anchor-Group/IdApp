using System;
using Xamarin.Forms;

namespace IdApp.Behaviors
{
    /// <summary>
    /// Used for moving focus to the next UI component when a button has been clicked.
    /// </summary>
    public class ScrollToClickedBehavior : Behavior<Button>
    {
        /// <summary>
        /// The view to move focus to.
        /// </summary>
        [TypeConverter(typeof(ReferenceTypeConverter))]
        public View ScrollTo { get; set; }

        /// <inheritdoc/>
        protected override void OnAttachedTo(Button Button)
        {
			Button.Clicked += this.Button_Clicked;
            base.OnAttachedTo(Button);
        }

		/// <inheritdoc/>
		protected override void OnDetachingFrom(Button Button)
        {
            Button.Clicked -= this.Button_Clicked;
            base.OnDetachingFrom(Button);
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            MakeVisible(this.ScrollTo);
        }

        /// <summary>
        /// Scrolls to make an element visisble.
        /// </summary>
        /// <param name="Element">Element to make visible.</param>
        public static void MakeVisible(View Element)
		{
            Element Loop = Element.Parent;
            while (!(Loop is null) && !(Loop is ScrollView))
                Loop = Loop.Parent;

            (Loop as ScrollView)?.ScrollToAsync(Element, ScrollToPosition.MakeVisible, true);
        }
    }
}
