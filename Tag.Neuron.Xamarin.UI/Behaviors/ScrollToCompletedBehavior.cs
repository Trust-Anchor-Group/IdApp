using System;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.Behaviors
{
    /// <summary>
    /// Used for moving focus to the next UI component when the Enter/Return key has been hit.
    /// </summary>
    public class ScrollToCompletedBehavior : Behavior<Entry>
    {
        /// <summary>
        /// The view to move focus to.
        /// </summary>
        [TypeConverter(typeof(ReferenceTypeConverter))]
        public View ScrollTo { get; set; }

        /// <inheritdoc/>
        protected override void OnAttachedTo(Entry entry)
        {
            entry.Completed += Entry_Completed;
            base.OnAttachedTo(entry);
        }

        /// <inheritdoc/>
        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= Entry_Completed;
            base.OnDetachingFrom(entry);
        }

        private void Entry_Completed(object sender, EventArgs e)
        {
            View view = ScrollTo;
            Element Loop = view.Parent;
            while (!(Loop is null) && !(Loop is ScrollView))
                Loop = Loop.Parent;

            (Loop as ScrollView)?.ScrollToAsync(view, ScrollToPosition.MakeVisible, true);
        }
    }
}