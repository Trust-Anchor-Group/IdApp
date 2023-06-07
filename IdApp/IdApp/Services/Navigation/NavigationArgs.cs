using IdApp.Services.Navigation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(nameof(NavigationService))]

namespace IdApp.Services.Navigation
{
	/// <summary>
	/// </summary>
	public enum BackMethod
	{
		/// <summary>
		/// Takes in consideration the parent's navigation arguments
		/// By fedault it will be considered a <see cref="Pop"/>
		/// </summary>
		Inherited = 0,

		/// <summary>
		/// Goes back just one navigation level - the route ".."
		/// </summary>
		Pop = 1,

		/// <summary>
		/// Sets the <see cref="NavigationArgs.backCounter"/> to 1
		/// All pages with inherited method will increment the backCounter
		/// </summary>
		ToThisPage = 2,

		/// <summary>
		/// Sets the <see cref="NavigationArgs.backCounter"/> to 2
		/// All pages with inherited method will increment the backCounter
		/// </summary>
		ToParentPage = 3,

		/// <summary>
		/// Goes back to the main page - the route "///" + nameof(MainPage)
		/// </summary>
		ToMainPage = 4,
	}

	/// <summary>
	/// An base class holding page specific navigation parameters.
	/// </summary>
	public class NavigationArgs
    {
		private NavigationArgs parentArgs = null;
		private BackMethod backMethod = BackMethod.Inherited;
		private int backCounter = 0;
		private string uniqueId = null;

		/// <summary>
		/// Sets the reference to the main parent's <see cref="NavigationArgs"/>.
		/// </summary>
		public void SetBackArguments(BackMethod BackMethod, NavigationArgs ParentArgs, string UniqueId)
		{
			this.backMethod = BackMethod;
			this.parentArgs = ParentArgs;
			this.uniqueId = UniqueId;
		}

		/*
		/// <summary>
		/// If view has been initialized by the arguments.
		/// </summary>
		public bool ViewInitialized { get; set; }
		*/

		/// <summary>
		/// Sets how do we have to go back
		/// </summary>
		public BackMethod GetBackMethod() => this.backMethod;

		/// <summary>
		/// Set it to 1 to start a counter of the number of times to pop when going back.
		/// It will be incremented on every push using the <see cref="INavigationService.GoToAsync"/> method.
		/// </summary>
		public int GetBackCounter() => this.backCounter;

		/// <summary>
		/// An untique view identificator used to search the args of similar view types.
		/// </summary>
		public string GetUniqueId() => this.uniqueId;
	}
}
