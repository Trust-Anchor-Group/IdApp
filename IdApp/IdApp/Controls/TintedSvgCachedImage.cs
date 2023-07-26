using FFImageLoading.Transformations;
using FFImageLoading.Svg.Forms;
using Xamarin.Forms;

namespace IdApp.Controls
{
    class TintedSvgCachedImage : SvgCachedImage
    {
        public static readonly BindableProperty TintColorProperty = BindableProperty.Create(
			nameof(TintColor), typeof(Color), typeof(TintedSvgCachedImage), Color.Default, propertyChanged: OnTintColorPropertyChanged);

        public Color TintColor
        {
            get => (Color)this.GetValue(TintColorProperty);
            set => this.SetValue(TintColorProperty, value);
        }

        static void OnTintColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
			=> ((TintedSvgCachedImage)bindable).OnTintColorPropertyChanged();

        void OnTintColorPropertyChanged()
        {
            this.Transformations = new () {
				new TintTransformation()
				{
					HexColor = this.TintColor.ToHex(),
					EnableSolidColor = true
				}
			};
        }
    }
}
