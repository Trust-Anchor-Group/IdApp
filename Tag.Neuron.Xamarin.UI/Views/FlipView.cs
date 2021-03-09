using System.Threading.Tasks;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.Views
{
    /// <summary>
    /// The FlipView is a user control that holds two child controls: a <see cref="FrontView"/> and a <see cref="BackView"/>.
    /// It can be flipped like a coin when the user taps on it. In order to flip the view, simply call the <see cref="Flip"/> method.
    /// </summary>
    public class FlipView : ContentView
    {
        private const uint DurationInMs = 300;
        private static readonly Easing EasingIn = Easing.SinIn;
        private static readonly Easing EasingOut = Easing.SinOut;

        private readonly RelativeLayout contentHolder;
        private View frontView;
        private View backView;
        private bool isFlipped;
        private bool isFlipping;

        /// <summary>
        /// Creates an instance of the <see cref="FlipView"/> control.
        /// </summary>
        public FlipView()
        {
            contentHolder = new RelativeLayout();
            Content = contentHolder;
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty FrontViewProperty =
            BindableProperty.Create("FrontView", typeof(View), typeof(FlipView), default(View), propertyChanged: (b, oldValue, newValue) =>
            {
                FlipView fv = (FlipView)b;
                if (!(newValue is null))
                {
                    if (!(fv.frontView is null))
                    {
                        fv.contentHolder.Children.Remove(fv.frontView);
                    }
                    fv.frontView = (View)newValue;

                    fv.AddChild(fv.frontView);
                }
                else
                {
                    if (!(fv.frontView is null))
                    {
                        fv.contentHolder.Children.Remove(fv.frontView);
                    }

                    fv.frontView = null;
                }

                fv.OrganizeViews();
            });

        /// <summary>
        /// The view displayed on the 'front' side of this user control.
        /// </summary>
        public View FrontView
        {
            get { return (View)GetValue(FrontViewProperty); }
            set { SetValue(FrontViewProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty BackViewProperty =
            BindableProperty.Create("BackView", typeof(View), typeof(FlipView), default(View), propertyChanged: (b, oldValue, newValue) =>
            {
                FlipView fv = (FlipView)b;
                if (!(newValue is null))
                {
                    if (!(fv.backView is null))
                    {
                        fv.contentHolder.Children.Remove(fv.backView);
                    }
                    fv.backView = (View)newValue;

                    fv.AddChild(fv.backView);
                }
                else
                {
                    if (!(fv.backView is null))
                    {
                        fv.contentHolder.Children.Remove(fv.backView);
                    }

                    fv.backView = null;
                }

                fv.OrganizeViews();
            });

        /// <summary>
        /// The view displayed on the 'back' side of this user control.
        /// </summary>
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
            if (!(this.frontView is null) && !(this.backView is null) && this.contentHolder.Children.Count > 1)
            {
                int fi = this.contentHolder.Children.IndexOf(this.frontView);
                int bi = this.contentHolder.Children.IndexOf(this.backView);
                if (fi >= 0 && bi >= 0 && fi < bi)
                {
                    this.contentHolder.Children.Remove(this.frontView);
                    this.AddChild(this.frontView);
                }

                this.backView.IsVisible = false;
            }
        }

        /// <summary>
        /// Makes the user control flip from front to back, or the other way around.
        /// </summary>
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
            this.CancelAnimations();
            this.RotationY = 360;
            await this.RotateYTo(270, DurationInMs, EasingIn);
        }

        private async Task RotateBackToFront_Forward()
        {
            this.CancelAnimations();
            this.RotationY = 90;
            await this.RotateYTo(0, DurationInMs, EasingOut);
        }

        private async Task RotateFrontToBack_Reverse()
        {
            this.CancelAnimations();
            this.RotationY = 0;
            await this.RotateYTo(90, DurationInMs, EasingIn);
        }

        private async Task RotateBackToFront_Reverse()
        {
            this.CancelAnimations();
            this.RotationY = 270;
            await this.RotateYTo(360, DurationInMs, EasingOut);
        }

        #endregion
    }
}