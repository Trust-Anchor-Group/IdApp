using System;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.Behaviors
{
    /// <summary>
    /// Used for moving focus to the next UI component when an item has been selected.
    /// </summary>
    public class ScrollToSelectedBehavior : Behavior<Picker>
    {
        /// <summary>
        /// The view to move focus to.
        /// </summary>
        [TypeConverter(typeof(ReferenceTypeConverter))]
        public View ScrollTo { get; set; }

        /// <inheritdoc/>
        protected override void OnAttachedTo(Picker Picker)
        {
			Picker.SelectedIndexChanged += Picker_SelectedIndexChanged;
            base.OnAttachedTo(Picker);
        }

		/// <inheritdoc/>
		protected override void OnDetachingFrom(Picker Picker)
        {
            Picker.SelectedIndexChanged -= Picker_SelectedIndexChanged;
            base.OnDetachingFrom(Picker);
        }

        private void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            View view = ScrollTo;
            Element Loop = view.Parent;
            while (!(Loop is null) && !(Loop is ScrollView))
                Loop = Loop.Parent;

            (Loop as ScrollView)?.ScrollToAsync(view, ScrollToPosition.MakeVisible, true);
        }
    }
}