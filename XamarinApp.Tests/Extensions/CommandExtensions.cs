using System.Windows.Input;

namespace XamarinApp.Tests.Extensions
{
    public static class CommandExtensions
    {
        public static bool IsDisabled(this ICommand command)
        {
            return command != null && command.CanExecute(null) == false;
        }

        public static bool IsEnabled(this ICommand command)
        {
            return !IsDisabled(command);
        }

        public static void IsExecuted(this ICommand command)
        {
            command.Execute(null);
        }
    }
}
