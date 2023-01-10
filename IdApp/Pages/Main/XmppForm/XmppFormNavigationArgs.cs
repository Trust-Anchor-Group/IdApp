using IdApp.Services.Navigation;
using Waher.Networking.XMPP.DataForms;

namespace IdApp.Pages.Main.XmppForm
{
	/// <summary>
	/// Holds navigation parameters for an XMPP Form.
	/// </summary>
	public class XmppFormNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Holds navigation parameters for an XMPP Form.
		/// </summary>
		public XmppFormNavigationArgs()
			: this(null)
		{
		}

		/// <summary>
		/// Holds navigation parameters for an XMPP Form.
		/// </summary>
		/// <param name="Form">XMPP Data Form</param>
		public XmppFormNavigationArgs(DataForm Form)
		{
			this.Form = Form;
		}

		/// <summary>
		/// XMPP Data Form
		/// </summary>
		public DataForm Form { get; }
	}
}
