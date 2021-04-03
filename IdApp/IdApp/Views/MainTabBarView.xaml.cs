using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin.Services;
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
        private readonly ILogService logService;
        private bool isShowing;

        /// <summary>
        /// Creates a new instance of the <see cref="MainTabBarView"/> class.
        /// </summary>
        public MainTabBarView()
        {
            this.logService = DependencyService.Resolve<ILogService>();
            // Set default values here.
            OnPlatform<string> fontFamily = (OnPlatform<string>)Application.Current.Resources["FontAwesomeSolid"];
            this.LeftButton1FontFamily = fontFamily;
            this.LeftButton2FontFamily = fontFamily;
            this.CenterButtonFontFamily = fontFamily;
            this.RightButtonFontFamily = fontFamily;

            InitializeComponent();

            LeftButton1.SetBinding(Button.CommandProperty, new Binding(nameof(LeftButton1Command), source: this));
            LeftButton2.SetBinding(Button.CommandProperty, new Binding(nameof(LeftButton2Command), source: this));
            CenterButton.SetBinding(Button.CommandProperty, new Binding(nameof(CenterButtonCommand), source: this));
            RightButton.SetBinding(Button.CommandProperty, new Binding(nameof(RightButtonCommand), source: this));

            LeftButton1.SetBinding(Button.FontFamilyProperty, new Binding(nameof(LeftButton1FontFamily), source: this));
            LeftButton2.SetBinding(Button.FontFamilyProperty, new Binding(nameof(LeftButton2FontFamily), source: this));
            CenterButton.SetBinding(Button.FontFamilyProperty, new Binding(nameof(CenterButtonFontFamily), source: this));
            RightButton.SetBinding(Button.FontFamilyProperty, new Binding(nameof(RightButtonFontFamily), source: this));

            LeftButton1.SetBinding(Button.TextProperty, new Binding(nameof(LeftButton1Text), source: this));
            LeftButton2.SetBinding(Button.TextProperty, new Binding(nameof(LeftButton2Text), source: this));
            CenterButton.SetBinding(Button.TextProperty, new Binding(nameof(CenterButtonText), source: this));
            RightButton.SetBinding(Button.TextProperty, new Binding(nameof(RightButtonText), source: this));
        }

        #region Properties

        /// <summary>
        /// See <see cref="LeftButton1Command"/>
        /// </summary>
        public static readonly BindableProperty LeftButton1CommandProperty =
            BindableProperty.Create("LeftButton1Command", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the first button.
        /// </summary>
        public ICommand LeftButton1Command
        {
            get { return (ICommand)GetValue(LeftButton1CommandProperty); }
            set { SetValue(LeftButton1CommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="LeftButton2Command"/>
        /// </summary>
        public static readonly BindableProperty LeftButton2CommandProperty =
            BindableProperty.Create("LeftButton2Command", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the first button.
        /// </summary>
        public ICommand LeftButton2Command
        {
            get { return (ICommand)GetValue(LeftButton2CommandProperty); }
            set { SetValue(LeftButton2CommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="CenterButtonCommand"/>
        /// </summary>
        public static readonly BindableProperty CenterButtonCommandProperty =
            BindableProperty.Create("CenterButtonCommand", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the second button.
        /// </summary>
        public ICommand CenterButtonCommand
        {
            get { return (ICommand)GetValue(CenterButtonCommandProperty); }
            set { SetValue(CenterButtonCommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="RightButtonCommand"/>
        /// </summary>
        public static readonly BindableProperty RightButtonCommandProperty =
            BindableProperty.Create("RightButtonCommand", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the third button.
        /// </summary>
        public ICommand RightButtonCommand
        {
            get { return (ICommand)GetValue(RightButtonCommandProperty); }
            set { SetValue(RightButtonCommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="LeftButton1FontFamily"/>
        /// </summary>
        public static readonly BindableProperty LeftButton1FontFamilyProperty =
            BindableProperty.Create("LeftButton1FontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the first button
        /// </summary>
        public string LeftButton1FontFamily
        {
            get { return (string)GetValue(LeftButton1FontFamilyProperty); }
            set { SetValue(LeftButton1FontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="LeftButton2FontFamily"/>
        /// </summary>
        public static readonly BindableProperty LeftButton2FontFamilyProperty =
            BindableProperty.Create("LeftButton2FontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the first button
        /// </summary>
        public string LeftButton2FontFamily
        {
            get { return (string)GetValue(LeftButton2FontFamilyProperty); }
            set { SetValue(LeftButton2FontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="CenterButtonFontFamily"/>
        /// </summary>
        public static readonly BindableProperty CenterButtonFontFamilyProperty =
            BindableProperty.Create("CenterButtonFontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the second button
        /// </summary>
        public string CenterButtonFontFamily
        {
            get { return (string)GetValue(CenterButtonFontFamilyProperty); }
            set { SetValue(CenterButtonFontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="RightButtonFontFamily"/>
        /// </summary>
        public static readonly BindableProperty RightButtonFontFamilyProperty =
            BindableProperty.Create("RightButtonFontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the third button
        /// </summary>
        public string RightButtonFontFamily
        {
            get { return (string)GetValue(RightButtonFontFamilyProperty); }
            set { SetValue(RightButtonFontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="LeftButton1Text"/>
        /// </summary>
        public static readonly BindableProperty LeftButton1TextProperty =
            BindableProperty.Create("LeftButton1Text", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the first button
        /// </summary>
        public string LeftButton1Text
        {
            get { return (string)GetValue(LeftButton1TextProperty); }
            set { SetValue(LeftButton1TextProperty, value); }
        }

        /// <summary>
        /// See <see cref="LeftButton2Text"/>
        /// </summary>
        public static readonly BindableProperty LeftButton2TextProperty =
            BindableProperty.Create("LeftButton2Text", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the first button
        /// </summary>
        public string LeftButton2Text
        {
            get { return (string)GetValue(LeftButton2TextProperty); }
            set { SetValue(LeftButton2TextProperty, value); }
        }

        /// <summary>
        /// See <see cref="CenterButtonText"/>
        /// </summary>
        public static readonly BindableProperty CenterButtonTextProperty =
            BindableProperty.Create("CenterButtonText", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the second button
        /// </summary>
        public string CenterButtonText
        {
            get { return (string)GetValue(CenterButtonTextProperty); }
            set { SetValue(CenterButtonTextProperty, value); }
        }

        /// <summary>
        /// See <see cref="RightButtonText"/>
        /// </summary>
        public static readonly BindableProperty RightButtonTextProperty =
            BindableProperty.Create("RightButtonText", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the third button
        /// </summary>
        public string RightButtonText
        {
            get { return (string)GetValue(RightButtonTextProperty); }
            set { SetValue(RightButtonTextProperty, value); }
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
                this.ToolBarContent.CancelAnimations();
                this.CenterButton.CancelAnimations();
                this.isShowing = true;
                Task translateToolBarTask = this.ToolBarContent.TranslateTo(0, 0, DurationInMs, Easing.SinIn);
                Task translateMiddleButtonTask = this.CenterButton.TranslateTo(0, 0, DurationInMs * 2, Easing.SinIn);
                try
                {
                    await Task.WhenAll(translateMiddleButtonTask, translateToolBarTask);
                }
                catch (TaskCanceledException)
                {
                    // Ok, do nothing
                }
                catch (Exception e)
                {
                    this.logService.LogException(e);   
                }
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
                this.isShowing = false;
                this.CenterButton.CancelAnimations();
                this.ToolBarContent.CancelAnimations();
                Task translateMiddleButtonTask = this.CenterButton.TranslateTo(0, 30, DurationInMs, Easing.SinOut);
                Task translateToolBarTask = this.ToolBarContent.TranslateTo(0, this.MainToolBar.Height, DurationInMs, Easing.SinOut);
                try
                {
                    await Task.WhenAll(translateToolBarTask, translateMiddleButtonTask);
                }
                catch (TaskCanceledException)
                {
                    // Ok, do nothing
                }
                catch (Exception e)
                {
                    this.logService.LogException(e);
                }
            }
        }
    }
}