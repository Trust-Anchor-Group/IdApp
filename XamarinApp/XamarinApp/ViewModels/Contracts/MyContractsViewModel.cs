using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Models;
using XamarinApp.Navigation;
using XamarinApp.Views.Contracts;

namespace XamarinApp.ViewModels.Contracts
{
    public class MyContractsViewModel : BaseViewModel
    {
        private readonly Dictionary<string, Contract> contractsMap;
        /// <summary>
        /// Show created contracts or signed contracts?
        /// </summary>
        private readonly bool showCreatedContracts;

        private readonly INeuronService neuronService;
        private readonly INetworkService networkService;
        private readonly INavigationService navigationService;
        private readonly IUiDispatcher uiDispatcher;
        private DateTime loadContractsTimestamp;

        public MyContractsViewModel(bool showCreatedContracts)
        {
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.networkService = DependencyService.Resolve<INetworkService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            this.showCreatedContracts = showCreatedContracts;
            this.contractsMap = new Dictionary<string, Contract>();
            this.Contracts = new ObservableCollection<ContractModel>();
            this.Title = showCreatedContracts ? AppResources.MyContracts : AppResources.SignedContracts;
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.ShowContractsMissing = false;
            this.loadContractsTimestamp = DateTime.UtcNow;
            _ = LoadContracts(this.loadContractsTimestamp);
        }

        protected override async Task DoUnbind()
        {
            this.ShowContractsMissing = false;
            this.loadContractsTimestamp = DateTime.UtcNow;
            this.Contracts.Clear();
            this.contractsMap.Clear();
            await base.DoUnbind();
        }

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create("Title", typeof(string), typeof(MyContractsViewModel), default(string));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly BindableProperty ShowContractsMissingProperty =
            BindableProperty.Create("ShowContractsMissing", typeof(bool), typeof(MyContractsViewModel), default(bool));

        public bool ShowContractsMissing
        {
            get { return (bool) GetValue(ShowContractsMissingProperty); }
            set { SetValue(ShowContractsMissingProperty, value); }
        }

        public ObservableCollection<ContractModel> Contracts { get; }

        public static readonly BindableProperty SelectedContractProperty =
            BindableProperty.Create("SelectedContract", typeof(ContractModel), typeof(MyContractsViewModel), default(ContractModel), propertyChanged: (b, oldValue, newValue) =>
            {
                MyContractsViewModel viewModel = (MyContractsViewModel)b;
                ContractModel model = (ContractModel)newValue;
                if (model != null && viewModel.contractsMap.TryGetValue(model.ContractId, out Contract contract))
                { 
                    viewModel.uiDispatcher.BeginInvokeOnMainThread(async () => await viewModel.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(contract, false)));
                }
            });

        public ContractModel SelectedContract
        {
            get { return (ContractModel)GetValue(SelectedContractProperty); }
            set { SetValue(SelectedContractProperty, value); }
        }

        private async Task LoadContracts(DateTime now)
        {
            string[] contractIds;

            if (this.showCreatedContracts)
            {
                (bool succeeded, string[] createdContractIds) = await this.networkService.TryRequest(this.neuronService.Contracts.GetCreatedContractsAsync);
                if (!succeeded)
                    return;
                contractIds = createdContractIds;
            }
            else
            {
                (bool succeeded, string[] signedContractIds) = await this.networkService.TryRequest(this.neuronService.Contracts.GetSignedContractsAsync);
                if (!succeeded)
                    return;
                contractIds = signedContractIds;
            }

            if (this.loadContractsTimestamp > now)
            {
                return;
            }

            if (contractIds.Length <= 0)
            {
                this.uiDispatcher.BeginInvokeOnMainThread(() => this.ShowContractsMissing = true);
                return;
            }

            foreach (string contractId in contractIds)
            {
                Contract contract = await this.neuronService.Contracts.GetContractAsync(contractId);
                if (this.loadContractsTimestamp > now)
                {
                    return;
                }
                this.contractsMap[contractId] = contract;
                ContractModel model = new ContractModel(contract.ContractId, contract.Created, $"{contract.ContractId} ({contract.ForMachinesNamespace}#{contract.ForMachinesLocalName})");
                this.Contracts.Add(model);
            }
        }
    }
}