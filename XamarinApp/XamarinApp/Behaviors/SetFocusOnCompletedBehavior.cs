using System;
using Xamarin.Forms;

namespace XamarinApp.Behaviors
{
    public class SetFocusOnCompletedBehavior : Behavior<Entry>
    {
        [TypeConverter(typeof(ReferenceTypeConverter))]
        public View SetFocusTo { get; set; }

        protected override void OnAttachedTo(Entry entry)
        {
            entry.Completed += Entry_Completed;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= Entry_Completed;
            base.OnDetachingFrom(entry);
        }

        void Entry_Completed(object sender, EventArgs e)
        {
            View view = SetFocusTo;
            if (view != null && view.IsVisible)
            {
                view.Focus();
            }
        }
    }
}