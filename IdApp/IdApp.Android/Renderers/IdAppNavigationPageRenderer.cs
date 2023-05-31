using System;
using Android.Content;
using IdApp.Pages;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android.AppCompat;
using AView = Android.Views.View;

[assembly: ExportRenderer(typeof(NavigationPage), typeof(IdApp.Android.Renderers.IdAppNavigationPageRenderer))]
namespace IdApp.Android.Renderers
{
    public class IdAppNavigationPageRenderer : NavigationPageRenderer, AView.IOnClickListener
    {
        public IdAppNavigationPageRenderer(Context Context) : base(Context)
        {
        }

        async void IOnClickListener.OnClick(AView AView)
        {
            try
            {
                Page CurrentPage = App.Current.MainPage;
                if (CurrentPage is NavigationPage NavigationPage)
                {
                    CurrentPage = NavigationPage.CurrentPage;
                }

                if (CurrentPage is not ContentBasePage ContentBasePage || !ContentBasePage.OnToolbarBackButtonPressed())
                    this.OnClick(AView);
            }
            catch (Exception Exception)
            {
                await App.Current.MainPage.DisplayAlert("IdAppNavigationPageRenderer.OnClick exception", Exception.Message, "OK");
            }
        }
    }
}