using System;

namespace XamarinApp
{
	/// <summary>
	/// Interface for pages having a back button.
	/// </summary>
	public interface IBackButton
	{
		/// <summary>
		/// Is called when the system back button has been clicked.
		/// </summary>
		/// <returns>If the back click event was handled or not.</returns>
		bool BackClicked();
	}
}
