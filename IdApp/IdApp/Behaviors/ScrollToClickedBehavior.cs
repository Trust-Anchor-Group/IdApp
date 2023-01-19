using IdApp.Pages.Registration.ValidateContactInfo;
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

		/// <summary>
		/// A BindableProperty for <see cref="Condition"/> property.
		/// </summary>
		public static readonly BindableProperty ConditionProperty =
			BindableProperty.Create(nameof(Condition), typeof(bool), typeof(ScrollToClickedBehavior), true);

		/// <summary>
		/// Gets or sets a value indicating if the user needs to provide a purpose.
		/// </summary>
		public bool Condition
		{
			get => (bool)this.GetValue(ConditionProperty);
			set => this.SetValue(ConditionProperty, value);
		}

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
			if(this.Condition)
				MakeVisible(this.ScrollTo);
        }

        /// <summary>
        /// Scrolls to make an element visisble.
        /// </summary>
        /// <param name="Element">Element to make visible.</param>
        public static void MakeVisible(View Element)
		{
            Element Loop = Element.Parent;
            while (Loop is not null && !(Loop is ScrollView))
                Loop = Loop.Parent;

            (Loop as ScrollView)?.ScrollToAsync(Element, ScrollToPosition.MakeVisible, true);
        }
    }
}
