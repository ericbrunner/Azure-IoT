using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Zone.IoT.Models;

namespace IoTShellApp.ViewModels
{
    internal class IoTMonitorViewModel : IAsyncDisposable
    {
        private HttpClient client
        {
            get
            {
                if (Device.RuntimePlatform == Device.Android)
                {
                    string[] token = DeviceInfo.VersionString.Split('.');
                    var number = token.FirstOrDefault();
                    var version = 5;
                    if (number != null)
                    {
                        version = int.Parse(number);
                    }

                    // see https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/http-stack?tabs=windows#choosing-a-handler
                    return version < 5 ? new HttpClient(new HttpClientHandler()) : new HttpClient();
                }
                else
                {
                    return new HttpClient();
                }
            }
        }

        private const string DeviceId = "MXCHIP";
        private const string UrlRoot = "zone-fx.azurewebsites.net";
        private const string ButtonAPressedText = "Button A Pressed";
        private const string ButtonANotPressedText = "Button A NOT Pressed";
        private const string TemperatureAlertText = "TemperatureAlert";
        private const string TemperatureNoAlertText = "No Alert";
        private const string DeviceShakedText = "DEVICE SHAKED!!!";
        private const string DeviceNotShakedText = "";

        private readonly string _devkitGetDataUrl =
            $"https://{UrlRoot}/api/devkitgetdata/{DeviceId}?code=";

        private readonly string _devkitSetDataUrl =
            $"https://{UrlRoot}/api/devkitsetdata/{DeviceId}?code=";

        public ICommand ClearCommand { get; }

        public IoTMonitorViewModel()
        {
            ClearCommand = new Command(() => DevkitData.Clear());
        }

        public async IAsyncEnumerable<DevkitData> GetDataAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(3000, cancellationToken);

                // Get the current page of results and parse them
                HttpResponseMessage response = await client.GetAsync(_devkitGetDataUrl, cancellationToken);

                if (!response.IsSuccessStatusCode) continue;
                
                string content = await response.Content.ReadAsStringAsync();

                cancellationToken.ThrowIfCancellationRequested();
                yield return JsonConvert.DeserializeObject<DevkitData>(content);
            }
        }

        private IAsyncEnumerable<DevkitData> _asyncEnumerable;
        private Task _processDataTask;
        public ObservableCollection<DevkitData> DevkitData { get; } = new ObservableCollection<DevkitData>();
        private CancellationTokenSource cts;
        private async Task ProcessDataAsync()
        {
            try
            {
                await foreach (DevkitData data in _asyncEnumerable)
                {
                    DevkitData.Add(data);
                    MessagingCenter.Send<object, object>(this, "MessageReceived", data);
                }
            }
            catch (Exception e)
            {
                Crashes.TrackError(e);
            }

        }

        public Task StartAsync()
        {
            cts = new CancellationTokenSource();
            _asyncEnumerable = GetDataAsync(cts.Token);
            _processDataTask = ProcessDataAsync();

            return Task.CompletedTask;
        }
        public async ValueTask DisposeAsync()
        {
            try
            {
                cts.Cancel();
                await _processDataTask;
            }
            catch (Exception e)
            {
                Crashes.TrackError(e);
            }
        }



    }
}