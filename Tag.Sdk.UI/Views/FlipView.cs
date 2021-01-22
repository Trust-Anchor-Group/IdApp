using System.Threading.Tasks;
using Xamarin.Forms;

namespace Tag.Sdk.UI.Views
{
    public class FlipView : ContentView
    {
        private const uint duration = 300;
        private static readonly Easing easingIn = Easing.SinIn;
        private static readonly Easing easingOut = Easing.SinOut;

        private readonly RelativeLayout contentHolder;
        private View frontView;
        private View backView;
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
                    if (fv.frontView != null)
                    {
                        fv.contentHolder.Children.Remove(fv.frontView);
                    }
                    fv.frontView = (View)newValue;

                    fv.AddChild(fv.frontView);
                }
                else
                {
                    if (fv.frontView != null)
                    {
                        fv.contentHolder.Children.Remove(fv.frontView);
                    }

                    fv.frontView = null;
                }

                fv.OrganizeViews();
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
                    if (fv.backView != null)
                    {
                        fv.contentHolder.Children.Remove(fv.backView);
                    }
                    fv.backView = (View)newValue;

                    fv.AddChild(fv.backView);
                }
                else
                {
                    if (fv.backView != null)
                    {
                        fv.contentHolder.Children.Remove(fv.backView);
                    }

                    fv.backView = null;
                }

                fv.OrganizeViews();
            });

        public View BackView
        {
            get { return (View)GetValue(BackViewProperty); }
            set { SetValue(BackViewProperty, value); }
        }

        private void AddChild(View childView)
        {
            this.contentHolder.Children
                .Add(childView,
                    Constraint.Constant(0),
                    Constraint.Constant(0),
                    Constraint.RelativeToParent((parent) => parent.Width),
                    Constraint.RelativeToParent((parent) => parent.Height)
                );
        }

        private void OrganizeViews()
        {
            if (this.frontView != null && this.backView != null && this.contentHolder.Children.Count > 1)
            {
                int fi = this.contentHolder.Children.IndexOf(this.frontView);
                int bi = this.contentHolder.Children.IndexOf(this.backView);
                if (fi >= 0 && bi >= 0 && fi < bi)
                {
                    this.contentHolder.Children.Remove(this.frontView);
                    this.AddChild(this.frontView);
                }
            }
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
            await RotateFrontToBack_Forward();

            // Change the visible content
            this.FrontView.IsVisible = false;
            this.BackView.IsVisible = true;

            await RotateBackToFront_Forward();
            this.isFlipping = false;
        }

        private async void FlipFromBackToFront()
        {
            this.isFlipping = true;
            await RotateFrontToBack_Reverse();

            // Change the visible content
            this.BackView.IsVisible = false;
            this.FrontView.IsVisible = true;

            await RotateBackToFront_Reverse();
            this.isFlipping = false;
        }

        #region Animation

        private async Task RotateFrontToBack_Forward()
        {
            ViewExtensions.CancelAnimations(this);
            this.RotationY = 360;
            await this.RotateYTo(270, duration, easingIn);
        }

        private async Task RotateBackToFront_Forward()
        {
            ViewExtensions.CancelAnimations(this);
            this.RotationY = 90;
            await this.RotateYTo(0, duration, easingOut);
        }

        private async Task RotateFrontToBack_Reverse()
        {
            ViewExtensions.CancelAnimations(this);
            this.RotationY = 0;
            await this.RotateYTo(90, duration, easingIn);
        }

        private async Task RotateBackToFront_Reverse()
        {
            ViewExtensions.CancelAnimations(this);
            this.RotationY = 270;
            await this.RotateYTo(360, duration, easingOut);
        }

        #endregion
    }
}