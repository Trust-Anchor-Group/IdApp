using System.Windows.Input;
using Xamarin.Forms;

namespace IdApp.Behaviors
{
    /// <summary>
    /// A behavior for being able to bind an <see cref="Entry"/>'s TextChanged event to an <see cref="ICommand"/>.
    /// </summary>
    public class TextChangedBehavior : Behavior<Entry>
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty TextChangedCommandProperty =
            BindableProperty.Create(nameof(TextChangedCommand), typeof(ICommand), typeof(TextChangedBehavior), default(ICommand));

        /// <summary>
        /// The command to bind to when text changes.
        /// </summary>
        public ICommand TextChangedCommand
        {
            get => (ICommand)this.GetValue(TextChangedCommandProperty);
            set => this.SetValue(TextChangedCommandProperty, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty TextChangedCommandParameterProperty =
            BindableProperty.Create(nameof(TextChangedCommandParameter), typeof(object), typeof(TextChangedBehavior), default);

        /// <summary>
        /// The command parameter to bind to when text changes.
        /// </summary>
        public object TextChangedCommandParameter
        {
            get => (object)this.GetValue(TextChangedCommandParameterProperty);
            set => this.SetValue(TextChangedCommandParameterProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += this.Entry_TextChanged;
            base.OnAttachedTo(entry);
        }

        /// <inheritdoc/>
        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= this.Entry_TextChanged;
            base.OnDetachingFrom(entry);
        }

        void Entry_TextChanged(object Sender, TextChangedEventArgs e)
        {
            if (this.TextChangedCommand is not null && this.TextChangedCommand.CanExecute(null))
            {
				this.TextChangedCommand.Execute(this.TextChangedCommandParameter);
            }
        }
    }
}
