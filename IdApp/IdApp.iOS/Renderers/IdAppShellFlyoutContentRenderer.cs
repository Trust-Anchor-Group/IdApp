using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace IdApp.iOS.Renderers
{
	internal class IdAppShellFlyoutContentRenderer : ShellFlyoutContentRenderer
	{
		public ShellTableViewController TableViewController;

		public IdAppShellFlyoutContentRenderer(IShellContext ShellContext) : base(ShellContext)
		{
		}

		protected override ShellTableViewController CreateShellTableViewController()
		{
			this.TableViewController = base.CreateShellTableViewController();
			return this.TableViewController;
		}
	}
}
