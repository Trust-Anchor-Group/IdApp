using System;
using Xamarin.Forms;

namespace IdApp.Behaviors
{
    /// <summary>
    /// Used for Re-rendering a layout, when a button has been clicked.
    /// </summary>
    public class ReRenderOnClickedBehavior : Behavior<Button>
    {
        /// <summary>
        /// The layout to re-render.
        /// </summary>
        [TypeConverter(typeof(ReferenceTypeConverter))]
        public Layout Layout { get; set; }

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

        private void Button_Clicked(object Sender, EventArgs e)
        {
            this.Layout?.ForceLayout();
        }
    }
}
