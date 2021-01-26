using System.Windows.Input;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.Extensions
{
    public static class CommandExtensions
    {
        public static void ChangeCanExecute(this ICommand command)
        {
            if (command is Command cmd)
            {
                cmd.ChangeCanExecute();
            }
        }

        public static void Execute(this ICommand command)
        {
            if (command != null && command.CanExecute(null))
            {
                command?.Execute(null);
            }
        }
    }
}