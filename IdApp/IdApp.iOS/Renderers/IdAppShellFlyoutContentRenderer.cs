using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace IdApp.iOS.Renderers
{
	internal class IdAppShellFlyoutContentRenderer : ShellFlyoutContentRenderer
	{
		private readonly UITableView flyoutTable;

		public IdAppShellFlyoutContentRenderer(IShellContext ShellContext) : base(ShellContext)
		{
			if (this.ChildViewControllers.OfType<ShellTableViewController>().FirstOrDefault() is ShellTableViewController Controller)
			{
				this.flyoutTable = Controller.TableView;
				ShellContext.Shell.PropertyChanged += this.OnShellPropertyChanged;
			}
		}

		private void OnShellPropertyChanged(object Sender, PropertyChangedEventArgs Args)
		{
			Shell Shell = (Shell)Sender;
			if (Args.PropertyName == nameof(Shell.FlyoutIsPresented) && Shell.FlyoutIsPresented)
			{
				Shell.PropertyChanged -= this.OnShellPropertyChanged;

				// This is a work around for https://github.com/xamarin/Xamarin.Forms/issues/14935.
				if (Shell.CurrentItem is ShellItem CurrentItem && this.flyoutTable.IndexPathForSelectedRow is null)
				{
					List<List<Element>> ShellGroupings = ((IShellController)Shell).GenerateFlyoutGrouping();

					if (TryFindShellItemPath(CurrentItem, ShellGroupings) is NSIndexPath CurrentItemPath)
					{
						this.flyoutTable.SelectRow(CurrentItemPath, animated: false, scrollPosition: UITableViewScrollPosition.None);

						if (this.flyoutTable.CellAt(CurrentItemPath) is UIContainerCell CurrentItemCell && CurrentItemCell.View is View CurrentItemView)
						{
							VisualStateManager.GoToState(CurrentItemView, "Selected");
						}
					}
				}
			}
		}

		private static NSIndexPath TryFindShellItemPath(ShellItem ShellItem, List<List<Element>> ShellGroupings)
		{
			bool Found = false;
			int Section;
			int Row = 0;

			for (Section = 0; Section < ShellGroupings.Count; Section++)
			{
				for (Row = 0; Row < ShellGroupings[Section].Count; Row++)
				{
					if (ShellGroupings[Section][Row] == ShellItem)
					{
						Found = true;
						break;
					}
				}

				if (Found)
				{
					break;
				}
			}

			return Found ? NSIndexPath.Create(Section, Row) : null;
		}
	}
}
