using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ContractsMenuPage : IBackButton
    {
        private readonly TagServiceSettings tagSettings;
        private readonly ITagService tagService;
        private readonly Page owner;

        public ContractsMenuPage(Page Owner)
        {
            InitializeComponent();
            this.tagSettings = DependencyService.Resolve<TagServiceSettings>();
            this.tagService = DependencyService.Resolve<ITagService>();
            this.owner = Owner;
            this.BindingContext = this;
        }

        private void CreatedContractsButton_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(new MyContractsPage(App.CurrentPage, true), false);
        }

        private void SignedContractsButton_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(new MyContractsPage(App.CurrentPage, false), false);
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
            this.BackButton_Clicked(this, EventArgs.Empty);
            return true;
        }

    }
}