using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
    /// <summary>
    /// Represents the main tab bar in the Main page of the application.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainTabBarView
    {
        private const uint DurationInMs = 250;

        private bool isShowing;

        /// <summary>
        /// Creates a new instance of the <see cref="MainTabBarView"/> class.
        /// </summary>
        public MainTabBarView()
        {
            // Set default values here.
            OnPlatform<string> fontFamily = (OnPlatform<string>)Application.Current.Resources["FontAwesomeSolid"];
            this.Button1FontFamily = fontFamily;
            this.Button2FontFamily = fontFamily;
            this.Button3FontFamily = fontFamily;
            this.Button4FontFamily = fontFamily;
            this.Button5FontFamily = fontFamily;

            InitializeComponent();

            Button1.SetBinding(Button.CommandProperty, new Binding(nameof(Button1Command), source: this));
            Button2.SetBinding(Button.CommandProperty, new Binding(nameof(Button2Command), source: this));
            Button3.SetBinding(Button.CommandProperty, new Binding(nameof(Button3Command), source: this));
            Button4.SetBinding(Button.CommandProperty, new Binding(nameof(Button4Command), source: this));
            Button5.SetBinding(Button.CommandProperty, new Binding(nameof(Button5Command), source: this));

            Button1.SetBinding(Button.FontFamilyProperty, new Binding(nameof(Button1FontFamily), source: this));
            Button2.SetBinding(Button.FontFamilyProperty, new Binding(nameof(Button2FontFamily), source: this));
            Button3.SetBinding(Button.FontFamilyProperty, new Binding(nameof(Button3FontFamily), source: this));
            Button4.SetBinding(Button.FontFamilyProperty, new Binding(nameof(Button4FontFamily), source: this));
            Button5.SetBinding(Button.FontFamilyProperty, new Binding(nameof(Button5FontFamily), source: this));

            Button1.SetBinding(Button.TextProperty, new Binding(nameof(Button1Text), source: this));
            Button2.SetBinding(Button.TextProperty, new Binding(nameof(Button2Text), source: this));
            Button3.SetBinding(Button.TextProperty, new Binding(nameof(Button3Text), source: this));
            Button4.SetBinding(Button.TextProperty, new Binding(nameof(Button4Text), source: this));
            Button5.SetBinding(Button.TextProperty, new Binding(nameof(Button5Text), source: this));
        }

        #region Properties

        /// <summary>
        /// See <see cref="Button1Command"/>
        /// </summary>
        public static readonly BindableProperty Button1CommandProperty =
            BindableProperty.Create("Button1Command", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the first button.
        /// </summary>
        public ICommand Button1Command
        {
            get { return (ICommand)GetValue(Button1CommandProperty); }
            set { SetValue(Button1CommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button2Command"/>
        /// </summary>
        public static readonly BindableProperty Button2CommandProperty =
            BindableProperty.Create("Button2Command", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the second button.
        /// </summary>
        public ICommand Button2Command
        {
            get { return (ICommand)GetValue(Button2CommandProperty); }
            set { SetValue(Button2CommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button3Command"/>
        /// </summary>
        public static readonly BindableProperty Button3CommandProperty =
            BindableProperty.Create("Button3Command", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the third button.
        /// </summary>
        public ICommand Button3Command
        {
            get { return (ICommand)GetValue(Button3CommandProperty); }
            set { SetValue(Button3CommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button4Command"/>
        /// </summary>
        public static readonly BindableProperty Button4CommandProperty =
            BindableProperty.Create("Button4Command", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the fourth button.
        /// </summary>
        public ICommand Button4Command
        {
            get { return (ICommand)GetValue(Button4CommandProperty); }
            set { SetValue(Button4CommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button5Command"/>
        /// </summary>
        public static readonly BindableProperty Button5CommandProperty =
            BindableProperty.Create("Button5Command", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the fifth button.
        /// </summary>
        public ICommand Button5Command
        {
            get { return (ICommand)GetValue(Button5CommandProperty); }
            set { SetValue(Button5CommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button1FontFamily"/>
        /// </summary>
        public static readonly BindableProperty Button1FontFamilyProperty =
            BindableProperty.Create("Button1FontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the first button
        /// </summary>
        public string Button1FontFamily
        {
            get { return (string)GetValue(Button1FontFamilyProperty); }
            set { SetValue(Button1FontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button2FontFamily"/>
        /// </summary>
        public static readonly BindableProperty Button2FontFamilyProperty =
            BindableProperty.Create("Button2FontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the second button
        /// </summary>
        public string Button2FontFamily
        {
            get { return (string)GetValue(Button2FontFamilyProperty); }
            set { SetValue(Button2FontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button3FontFamily"/>
        /// </summary>
        public static readonly BindableProperty Button3FontFamilyProperty =
            BindableProperty.Create("Button3FontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the third button
        /// </summary>
        public string Button3FontFamily
        {
            get { return (string)GetValue(Button3FontFamilyProperty); }
            set { SetValue(Button3FontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button4FontFamily"/>
        /// </summary>
        public static readonly BindableProperty Button4FontFamilyProperty =
            BindableProperty.Create("Button4FontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the fourth button
        /// </summary>
        public string Button4FontFamily
        {
            get { return (string)GetValue(Button4FontFamilyProperty); }
            set { SetValue(Button4FontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button5FontFamily"/>
        /// </summary>
        public static readonly BindableProperty Button5FontFamilyProperty =
            BindableProperty.Create("Button5FontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the fifth button
        /// </summary>
        public string Button5FontFamily
        {
            get { return (string)GetValue(Button5FontFamilyProperty); }
            set { SetValue(Button5FontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button1Text"/>
        /// </summary>
        public static readonly BindableProperty Button1TextProperty =
            BindableProperty.Create("Button1Text", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the first button
        /// </summary>
        public string Button1Text
        {
            get { return (string)GetValue(Button1TextProperty); }
            set { SetValue(Button1TextProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button2Text"/>
        /// </summary>
        public static readonly BindableProperty Button2TextProperty =
            BindableProperty.Create("Button2Text", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the second button
        /// </summary>
        public string Button2Text
        {
            get { return (string)GetValue(Button2TextProperty); }
            set { SetValue(Button2TextProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button3Text"/>
        /// </summary>
        public static readonly BindableProperty Button3TextProperty =
            BindableProperty.Create("Button3Text", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the third button
        /// </summary>
        public string Button3Text
        {
            get { return (string)GetValue(Button3TextProperty); }
            set { SetValue(Button3TextProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button4Text"/>
        /// </summary>
        public static readonly BindableProperty Button4TextProperty =
            BindableProperty.Create("Button4Text", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the fourth button
        /// </summary>
        public string Button4Text
        {
            get { return (string)GetValue(Button4TextProperty); }
            set { SetValue(Button4TextProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button5Text"/>
        /// </summary>
        public static readonly BindableProperty Button5TextProperty =
            BindableProperty.Create("Button5Text", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the fifth button
        /// </summary>
        public string Button5Text
        {
            get { return (string)GetValue(Button5TextProperty); }
            set { SetValue(Button5TextProperty, value); }
        }

        #endregion

        /// <summary>
        /// Call this method to show the toolbar content.
        /// </summary>
        /// <returns></returns>
        public async Task Show()
        {
            if (!this.isShowing)
            {
                this.isShowing = true;
                Task translateButton3Task = this.Button3.TranslateTo(0, 30, DurationInMs, Easing.SinOut);
                Task translateToolBarTask = this.ToolBarContent.TranslateTo(0, this.MainToolBar.Height, DurationInMs, Easing.SinOut);
                await Task.WhenAll(translateButton3Task, translateToolBarTask);
            }
        }

        /// <summary>
        /// Call this method to hide the toolbar content.
        /// </summary>
        /// <returns></returns>
        public async Task Hide()
        {
            if (this.isShowing)
            {
                Task translateToolBarTask = this.ToolBarContent.TranslateTo(0, 0, DurationInMs, Easing.SinIn);
                Task translateButton3Task = this.Button3.TranslateTo(0, 0, DurationInMs * 2, Easing.SinIn);
                await Task.WhenAll(translateToolBarTask, translateButton3Task);
                this.isShowing = false;
            }
        }
    }
}