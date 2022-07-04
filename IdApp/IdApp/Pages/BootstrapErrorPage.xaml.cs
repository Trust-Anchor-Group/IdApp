using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class BootstrapErrorPage : ContentBasePage
	{
		public BootstrapErrorPage()
		{
			InitializeComponent();
		}

		protected override bool OnBackButtonPressed()
		{
			return true;
		}
	}
}
