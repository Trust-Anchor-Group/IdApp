using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Shell), typeof(IdApp.iOS.Renderers.IdAppShellRenderer))]
namespace IdApp.iOS.Renderers
{
	public class IdAppShellRenderer : ShellRenderer
	{
		private IdAppShellFlyoutContentRenderer flyoutContentRenderer;

		protected override IShellFlyoutContentRenderer CreateShellFlyoutContentRenderer()
		{
			return this.flyoutContentRenderer = new IdAppShellFlyoutContentRenderer(this);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Shell.FlyoutIsPresentedProperty.PropertyName && ((Shell)sender).FlyoutIsPresented)
			{
				UITableView TableView = this.flyoutContentRenderer?.TableViewController.TableView;
				if (TableView != null)
				{
					NSIndexPath SelectedRowIndex = TableView?.IndexPathForSelectedRow;
					if (SelectedRowIndex == null)
					{
						TableView.SelectRow(NSIndexPath.Create(0, 0), animated: false, scrollPosition: UITableViewScrollPosition.None);
						SelectedRowIndex = TableView.IndexPathForSelectedRow;
					}

					UIContainerCell SelectedCell = (UIContainerCell)TableView.CellAt(SelectedRowIndex);
					if (SelectedCell.View is View CellView)
					{
						VisualStateManager.GoToState(CellView, "Selected");
					}
				}
			}
		}
	}
}
