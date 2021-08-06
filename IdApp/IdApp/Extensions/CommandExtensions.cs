using System.Windows.Input;
using Xamarin.Forms;

namespace IdApp.Extensions
{
    /// <summary>
    /// Helper/convenience methods for the <see cref="ICommand"/>.
    /// </summary>
    public static class CommandExtensions
    {
        /// <summary>
        /// Calls the <see cref="Command.ChangeCanExecute"/> method on the given <see cref="ICommand"/>, given that it <b>is</b> a <see cref="Command"/>.
        /// </summary>
        /// <param name="command"></param>
        public static void ChangeCanExecute(this ICommand command)
        {
            if (command is Command cmd)
                cmd.ChangeCanExecute();
        }

        /// <summary>
        /// Calls the <see cref="ICommand.Execute"/> method with a <c>null</c> argument, given that the command <em>can</em> be executed (<see cref="ICommand.CanExecute"/>).
        /// </summary>
        /// <param name="command"></param>
        public static void Execute(this ICommand command)
        {
            if (!(command is null) && command.CanExecute(null))
                command?.Execute(null);
        }
    }
}