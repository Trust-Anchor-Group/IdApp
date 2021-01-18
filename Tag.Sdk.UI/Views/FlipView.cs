using System.Threading.Tasks;
using Xamarin.Forms;

namespace Tag.Sdk.UI.Views
{
    public class FlipView : ContentView
    {
        private readonly RelativeLayout contentHolder;
        private bool isFlipped;
        private bool isFlipping;

        public FlipView()
        {
            contentHolder = new RelativeLayout();
            Content = contentHolder;
        }

        public static readonly BindableProperty FrontViewProperty =
            BindableProperty.Create("FrontView", typeof(View), typeof(FlipView), default(View), propertyChanged: (b, oldValue, newValue) =>
        {
            FlipView fv = (FlipView)b;
            if (newValue != null)
            {
                fv.contentHolder.Children
                    .Add((View)newValue,
                        Constraint.Constant(0),
                        Constraint.Constant(0),
                        Constraint.RelativeToParent((parent) => parent.Width),
                        Constraint.RelativeToParent((parent) => parent.Height)
                    );
            }
        });

        public View FrontView
        {
            get { return (View)GetValue(FrontViewProperty); }
            set { SetValue(FrontViewProperty, value); }
        }

        public static readonly BindableProperty BackViewProperty =
            BindableProperty.Create("BackView", typeof(View), typeof(FlipView), default(View), propertyChanged: (b, oldValue, newValue) =>
            {
                FlipView fv = (FlipView)b;
                if (newValue != null)
                {
                    fv.contentHolder.Children
                        .Add((View)newValue,
                            Constraint.Constant(0),
                            Constraint.Constant(0),
                            Constraint.RelativeToParent((parent) => parent.Width),
                            Constraint.RelativeToParent((parent) => parent.Height)
                        );
                }
            });

        public View BackView
        {
            get { return (View)GetValue(BackViewProperty); }
            set { SetValue(BackViewProperty, value); }
        }

        public void Flip()
        {
            if (!isFlipping)
            {
                if (this.isFlipped)
                {
                    this.FlipFromBackToFront();
                    this.isFlipped = false;
                }
                else
                {
                    this.FlipFromFrontToBack();
                    this.isFlipped = true;
                }
            }
        }

        private async void FlipFromFrontToBack()
        {
            this.isFlipping = true;
            await RotateFrontToBack();

            // Change the visible content
            this.FrontView.IsVisible = false;
            this.BackView.IsVisible = true;

            await RotateBackToFront();
            this.isFlipping = false;
        }

        private async void FlipFromBackToFront()
        {
            this.isFlipping = true;
            await RotateFrontToBack();

            // Change the visible content
            this.BackView.IsVisible = false;
            this.FrontView.IsVisible = true;

            await RotateBackToFront();
            this.isFlipping = false;
        }

        #region Animation

        private async Task<bool> RotateFrontToBack()
        {
            ViewExtensions.CancelAnimations(this);

            this.RotationY = 360;

            await this.RotateYTo(270, 500, Easing.Linear);

            return true;
        }

        private async Task<bool> RotateBackToFront()
        {
            ViewExtensions.CancelAnimations(this);

            this.RotationY = 90;

            await this.RotateYTo(0, 500, Easing.Linear);

            return true;
        }

        #endregion
    }
}