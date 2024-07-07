using System;
using IoTShellApp.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZoneApp.Services;

namespace IoTShellApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InterCommPage : ContentPage
    {

        private readonly IntercommViewModel ViewModel = IntercommViewModel.Instance;
        public InterCommPage()
        {
            InitializeComponent();
            BindingContext = ViewModel;
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Bootstrap SignalR Connection
                string host = "dev-zone.azurewebsites.net";
                bool useHttps  =true;
                ZoneSignalrService.Instance.Init(host, useHttps);
            }
            catch (Exception e)
            {
                await DisplayAlert("SignalR Init Error", e.Message, "Ok");
            }
        }

        private void ConnectButtonClicked(object sender, EventArgs e)
        {
            if (ViewModel.IsConnected) return;

            if (!DesignMode.IsDesignModeEnabled)
                ViewModel.ConnectCommand.Execute(null);
        }

        private void DisconnectButtonClicked(object sender, EventArgs e)
        {
            if (ViewModel.IsDisconnected) return;

            if (!DesignMode.IsDesignModeEnabled)
                ViewModel.DisconnectCommand.Execute(null);
        }
    }
}