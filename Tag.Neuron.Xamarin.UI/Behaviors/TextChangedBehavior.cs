﻿using System.Windows.Input;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.Behaviors
{
    /// <summary>
    /// A behavior for being able to bind an <see cref="Entry"/>'s TextChanged event to an <see cref="ICommand"/>.
    /// </summary>
    public class TextChangedBehavior : Behavior<Entry>
    {
        public static readonly BindableProperty TextChangedCommandProperty =
            BindableProperty.Create("TextChangedCommand", typeof(ICommand), typeof(TextChangedBehavior), default(ICommand));

        public ICommand TextChangedCommand
        {
            get { return (ICommand)GetValue(TextChangedCommandProperty); }
            set { SetValue(TextChangedCommandProperty, value); }
        }

        public static readonly BindableProperty TextChangedCommandParameterProperty =
            BindableProperty.Create("TextChangedCommandParameter", typeof(object), typeof(TextChangedBehavior), default(object));

        public object TextChangedCommandParameter
        {
            get { return (object) GetValue(TextChangedCommandParameterProperty); }
            set { SetValue(TextChangedCommandParameterProperty, value); }
        }

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
                TextChangedCommand.Execute(TextChangedCommandParameter);
            }
        }
    }
}
