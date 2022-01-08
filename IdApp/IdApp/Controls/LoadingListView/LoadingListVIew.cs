using System;
using System.Collections;
using System.Windows.Input;
using Xamarin.Forms;

namespace IdApp.Controls.LoadingListView
{
	/// <summary>
	/// ListView that can load new items when the last items is being displayed
	/// </summary>
	public class LoadingListView : ListView
	{
        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty LoadMoreCommandProperty =
            BindableProperty.Create("TextChangedCommand", typeof(ICommand), typeof(LoadingListView), default(ICommand));

        /// <summary>
        /// Command executed when last item is appearing and new data should be loaded.
        /// </summary>
        public ICommand LoadMoreCommand
        {
            get { return (ICommand)GetValue(LoadMoreCommandProperty); }
            set { SetValue(LoadMoreCommandProperty, value); }
        }

        /// <summary>
        /// ListView that can load new items when the last items is being displayed
        /// </summary>
        public LoadingListView()
        {
            ItemAppearing += LoadingListView_ItemAppearing;
        }

        private void LoadingListView_ItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            if (ItemsSource is IList List && e.Item == List[List.Count - 1])
            {
                if (this.LoadMoreCommand?.CanExecute(null) ?? false)
                    this.LoadMoreCommand.Execute(null);
            }
        }
    }
}
