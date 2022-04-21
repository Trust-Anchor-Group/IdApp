using System;
using System.Windows.Input;
using IdApp.Controls.NoBounceView;
using Xamarin.Forms;

namespace IdApp.Controls.LoadingCollectionView
{
    /// <summary>
    /// CollectionView that can load new items when the last items is being displayed
    /// </summary>
    public class LoadingCollectionView : NoBounceCollectionView
	{
        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty LoadMoreCommandProperty =
            BindableProperty.Create(nameof(LoadMoreCommand), typeof(ICommand), typeof(LoadingCollectionView), default(ICommand));

        /// <summary>
        /// Command executed when last item is appearing and new data should be loaded.
        /// </summary>
        public ICommand LoadMoreCommand
        {
            get { return (ICommand)this.GetValue(LoadMoreCommandProperty); }
            set { this.SetValue(LoadMoreCommandProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty ItemSelectedCommandProperty =
            BindableProperty.Create(nameof(ItemSelectedCommand), typeof(ICommand), typeof(LoadingCollectionView), default(ICommand));

        /// <summary>
        /// Command executed when last item is appearing and new data should be loaded.
        /// </summary>
        public ICommand ItemSelectedCommand
        {
            get { return (ICommand)this.GetValue(ItemSelectedCommandProperty); }
            set { this.SetValue(ItemSelectedCommandProperty, value); }
        }

        /// <summary>
        /// ListView that can load new items when the last items is being displayed
        /// </summary>
        public LoadingCollectionView()
        {
            this.RemainingItemsThresholdReached += LoadingCollectionView_ThresholdReached;
			this.SelectionChanged += LoadingCollectionView_SelectionChanged;
        }

		private void LoadingCollectionView_ThresholdReached(object sender, EventArgs e)
        {
            if (this.LoadMoreCommand?.CanExecute(null) ?? false)
                this.LoadMoreCommand.Execute(null);
        }

        private void LoadingCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ItemSelectedCommand?.CanExecute(null) ?? false)
                this.ItemSelectedCommand.Execute(this.SelectedItem);
        }

    }
}
