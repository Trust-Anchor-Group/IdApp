using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinApp.MainMenu.Contracts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ContractsMenuPage : ContentPage, IBackButton
    {
        private readonly Page owner;

        public ContractsMenuPage(Page Owner)
        {
            this.owner = Owner;
            this.BindingContext = this;
            InitializeComponent();
        }

        private void CreatedContractsButton_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(new MyContractsPage(App.Configuration, App.CurrentPage, true), false);
        }

        private void SignedContractsButton_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(new MyContractsPage(App.Configuration, App.CurrentPage, false), false);
        }

        private void NotaryButton_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(new NotaryMenuPage(App.CurrentPage), false);
        }

        private void BackButton_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(this.owner, true);
        }

        public bool BackClicked()
        {
            this.BackButton_Clicked(this, new EventArgs());
            return true;
        }

    }
}