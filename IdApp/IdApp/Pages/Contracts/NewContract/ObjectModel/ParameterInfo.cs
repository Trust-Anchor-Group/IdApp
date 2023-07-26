using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.Pages.Contracts.NewContract.ObjectModel
{
    /// <summary>
    /// Contains information about a parameter.
    /// </summary>
    public class ParameterInfo
    {
        /// <summary>
        /// Contains information about a parameter.
        /// </summary>
        /// <param name="Parameter">Contract parameter.</param>
        /// <param name="Control">Generated control.</param>
        public ParameterInfo(Parameter Parameter, View Control)
		{
            this.Parameter = Parameter;
            this.Control = Control;
		}

        /// <summary>
        /// Contract parameter.
        /// </summary>
        public Parameter Parameter { get; internal set; }

        /// <summary>
        /// Generated control.
        /// </summary>
        public View Control { get; internal set; }
    }
}
