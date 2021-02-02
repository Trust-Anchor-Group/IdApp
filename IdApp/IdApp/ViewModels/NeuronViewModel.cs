﻿using IdApp.Extensions;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP;
using Xamarin.Forms;

namespace IdApp.ViewModels
{
    public class NeuronViewModel : BaseViewModel
    {
        protected NeuronViewModel(INeuronService neuronService, IUiDispatcher uiDispatcher)
        {
            this.NeuronService = neuronService;
            this.UiDispatcher = uiDispatcher;
            this.ConnectionStateText = AppResources.XmppState_Offline;
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.SetConnectionStateAndText(this.NeuronService.State);
            this.NeuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        protected override async Task DoUnbind()
        {
            this.NeuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            await base.DoUnbind();
        }

        #region Properties

        protected IUiDispatcher UiDispatcher { get; }
        protected INeuronService NeuronService { get; }

        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(NeuronViewModel), default(string));

        public string ConnectionStateText
        {
            get { return (string)GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create("IsConnected", typeof(bool), typeof(NeuronViewModel), default(bool));

        public bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        #endregion

        protected virtual void SetConnectionStateAndText(XmppState state)
        {
            this.ConnectionStateText = state.ToDisplayText(null);
            this.IsConnected = state == XmppState.Connected;
        }

        protected virtual void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => this.SetConnectionStateAndText(e.State));
        }
    }
}