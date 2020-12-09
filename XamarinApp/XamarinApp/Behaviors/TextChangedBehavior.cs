using System.Windows.Input;
using Xamarin.Forms;

namespace XamarinApp.Behaviors
{
    public class TextChangedBehavior : Behavior<Entry>
    {
        public ICommand TextChangedCommand { get; set; }
        //public static readonly BindableProperty TextChangedCommandProperty =
        //    BindableProperty.Create("TextChangedCommand", typeof(ICommand), typeof(TextChangedBehavior), default(ICommand), BindingMode.OneWayToSource);

        //public ICommand TextChangedCommand
        //{
        //    get { return (ICommand)GetValue(TextChangedCommandProperty); }
        //    set { SetValue(TextChangedCommandProperty, value); }
        //}

        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += Entry_TextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= Entry_TextChanged;
            base.OnDetachingFrom(entry);
        }

        void Entry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextChangedCommand != null && TextChangedCommand.CanExecute(null))
            {
                TextChangedCommand.Execute(e.NewTextValue);
            }
        }
    }
}
