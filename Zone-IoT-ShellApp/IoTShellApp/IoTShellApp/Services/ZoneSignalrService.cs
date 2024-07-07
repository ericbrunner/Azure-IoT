using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IoTShellApp.Services;
using IoTShellApp.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Xamarin.Forms;


namespace ZoneApp.Services
{
    public class ZoneSignalrService
    {
        public event EventHandler<MessageEventArgs> ReceivedMessage;

        private HubConnection _hubConnection;
        private Random _random;

        public bool IsConnected => _hubConnection != null && _hubConnection.State == HubConnectionState.Connected;

        public static ZoneSignalrService Instance = new ZoneSignalrService();


        public void Init(string urlRoot, bool useHttps)
        {
            _random = new Random();

            Debug.WriteLine("SignalR: WSS creating ...");
            var url = $"http{(useHttps ? "s" : string.Empty)}://{urlRoot}/intercommHub";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(url)
                .ConfigureLogging(logging =>
                {
                    // Log to the Console
                    logging.AddConsole();

                    // This will set ALL logging to Debug level
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            Debug.WriteLine("SignalR: WSS created.");

            // give the server a bit of time until the client closes the hub connection
            _hubConnection.ServerTimeout = TimeSpan.FromMinutes(10);
            
            _hubConnection.Closed += _hubConnection_Closed;

            _hubConnection.On<string, string>("broadcastMessage", (name, message) =>
            {
                var finalMessage = $"{name} says {message}";
                Debug.WriteLine($"SignalR: RECEIVED Message in \"broadcastMessage\" => {finalMessage}");
                ReceivedMessage?.Invoke(this, new MessageEventArgs(finalMessage));
            });
        }

        private Task _hubConnection_Closed(Exception arg)
        {
            //Log.Logger.Error(arg, "Event Connection.Closed triggered. Something went wrong");
            Task t = TryReconnectAsync();
            return t;
        }
        private async Task TryReconnectAsync()
        {
            int attempt = 1;
            int msec = 10000;
            while (true)
            {
                try
                {
                    //Log.Logger.Information($"WAIT BEFORE RECONNECTED ATTEMPT: {msec} msec.");
                    Debug.WriteLine($"SignalR: WAIT BEFORE RECONNECTED ATTEMPT: {msec} msec.");
                    await Task.Delay(msec);

                    //Log.Logger.Information($"TRY TO RECONNECTED... Attempt: {attempt}");
                    Debug.WriteLine($"SignalR: TRY TO RECONNECTED... Attempt: {attempt}");
                    await _hubConnection.StartAsync();
                    //Log.Logger.Information($"RECONNECTED on Attempt: {attempt}");
                    Debug.WriteLine($"SignalR: RECONNECTED on Attempt: {attempt}");

                    break;
                }
                catch (Exception e)
                {
                    //Log.Logger.Error($"Exception in TryReconnect method. Attempt: {attempt++}", e);
                    Debug.WriteLine($"Exception in TryReconnect method. Attempt: {attempt++}", e);
                }
            }
        }
        public async Task ConnectAsync(IntercommViewModel viewmodel)
        {
            if (IsConnected)
                return;

            try
            {
                await _hubConnection.StartAsync();

                await SendMessageAsync("_SYSTEM_", $"{viewmodel.Username} JOINED", viewmodel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Device.BeginInvokeOnMainThread(() => { Application.Current.MainPage.DisplayAlert("Hub Connect Error", e.Message, "Ok"); });

                await DisconnectAsync(viewmodel);
            }
        }

        public async Task DisconnectAsync(IntercommViewModel viewModel)
        {
            if (!IsConnected)
                return;

            try
            {
                await SendMessageAsync("_SYSTEM_", $"{viewModel.Username} LEFT", viewModel);
                await _hubConnection.StopAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Device.BeginInvokeOnMainThread(() =>
                {
                    Application.Current.MainPage.DisplayAlert("Hub Disconnect Error", e.Message, "Ok");
                });
            }
        }


        public async Task SendMessageAsync(string name, string message, IntercommViewModel viewmodel)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            try
            {
                // try re-connect
                if (!IsConnected)
                {
                    await ConnectAsync(viewmodel);
                }

                if (!IsConnected)
                {
                    Console.WriteLine("Hub Client is still disconnected.");
                    return;
                }

                await _hubConnection.InvokeAsync("BroadcastMessage", name, message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Device.BeginInvokeOnMainThread(() => { Application.Current.MainPage.DisplayAlert("Hub Send Error", e.Message, "Ok"); });
            }
        }
    }
}