using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using IoTShellApp.Models;
using IoTShellApp.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZoneApp.Services;

namespace IoTShellApp.ViewModels
{
    public class IntercommViewModel : BaseViewModel, IDisposable
    {
        public static IntercommViewModel Instance = new IntercommViewModel();
        public ChatMessage ChatMessage { get; }

        private string _username;

        public string Username =>
            _username ?? (_username = $"{DeviceInfo.Manufacturer}-{DeviceInfo.Model}-v.{DeviceInfo.Version}");

        public ObservableCollection<ChatMessage> Messages { get; }

        private bool _isConnected;

        public bool IsDisconnected => !IsConnected;

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected == value)
                    return;

                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(IsDisconnected));
            }
        }


        public Command SendMessageCommand { get; }
        public Command ConnectCommand { get; }
        public Command DisconnectCommand { get; }

        private IntercommViewModel()
        {
            ChatMessage = new ChatMessage();
            Messages = new ObservableCollection<ChatMessage>();
            Messages.Add(new ChatMessage(){Message = "INIT"});
            SendMessageCommand = new Command(async () => await SendMessage());
            ConnectCommand = new Command(async () => await Connect());
            DisconnectCommand = new Command(async () => await Disconnect());

            ZoneSignalrService.Instance.ReceivedMessage += Instance_ReceivedMessage;
        }

        private void Instance_ReceivedMessage(object sender, MessageEventArgs e)
        {
            SendLocalMessage(e.Message);
        }

        private async Task Connect()
        {
            if (IsConnected)
                return;
            try
            {
                IsBusy = true;

                System.Diagnostics.Debug.WriteLine("SignalR: Hub connecting ...");
                SendLocalMessage("SignalR: Hub connecting ...");
                
                await ZoneSignalrService.Instance.ConnectAsync(viewmodel: this);


                IsConnected = ZoneSignalrService.Instance.IsConnected;

                System.Diagnostics.Debug.WriteLine("SignalR: Hub connected.");
                SendLocalMessage("SignalR: Hub connected.");
            }
            catch (Exception ex)
            {
                SendLocalMessage($"Connection error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task Disconnect()
        {
            if (!IsConnected)
                return;

            try
            {
                IsBusy = true;
                System.Diagnostics.Debug.WriteLine("SignalR: Hub disconnecting ...");
                SendLocalMessage("SignalR: Hub disconnecting ...");

                await ZoneSignalrService.Instance.DisconnectAsync(this);

                IsConnected = ZoneSignalrService.Instance.IsConnected;
                System.Diagnostics.Debug.WriteLine("SignalR: Hub disconnected.");
                SendLocalMessage("SignalR: Hub disconnected.");
            }
            catch (Exception e)
            {
                SendLocalMessage($"Connection error: {e.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SendMessage()
        {
            if (!IsConnected)
            {
                await Application.Current.MainPage.DisplayAlert("Not connected",
                    "Please connect to the server and try again.", "OK");
                return;
            }

            try
            {
                IsBusy = true;
                await ZoneSignalrService.Instance.SendMessageAsync(Username, ChatMessage.Message, viewmodel: this);

                ChatMessage.Message = string.Empty;
            }
            catch (Exception ex)
            {
                SendLocalMessage($"Send failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }


        private void SendLocalMessage(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Messages.Add(new ChatMessage
                {
                    Message = message
                });
            });
        }

        public void Dispose()
        {
            ZoneSignalrService.Instance.ReceivedMessage -= Instance_ReceivedMessage;
        }
    }
}
