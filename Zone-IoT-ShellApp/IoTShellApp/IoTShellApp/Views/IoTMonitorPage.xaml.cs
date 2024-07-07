using IoTShellApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IoTShellApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class IoTMonitorPage : ContentPage
    {
        private readonly IoTMonitorViewModel _viewModel = new IoTMonitorViewModel();
        public IoTMonitorPage()
        {
            InitializeComponent();

            BindingContext = _viewModel;
            MessagingCenter.Subscribe<object, object>(this, "MessageReceived", (sender, arg) => {
                ListView.ScrollTo(arg, ScrollToPosition.End, true);
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await _viewModel.StartAsync();
            }
            catch (Exception e)
            {

                await this.DisplayAlert("Start Error", e.Message, "Ok");
            }

        }

        protected override async void OnDisappearing()
        {
            try
            {
                await _viewModel.DisposeAsync();
            }
            catch (Exception e)
            {
                await this.DisplayAlert("Dispose Error", e.Message, "Ok");
            }


            base.OnDisappearing();
        }

    }
}