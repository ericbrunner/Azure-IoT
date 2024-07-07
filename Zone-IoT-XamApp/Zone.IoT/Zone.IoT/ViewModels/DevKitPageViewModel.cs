using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Xamarin.Forms;
using Zone.IoT.Models;

namespace Zone.IoT.ViewModels
{
    public class DevKitPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        void RaisePropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly Timer _timer;

        private HttpClient httpClient
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

        private const string DeviceId = "ERIC-MXCHIP-AZ3166";
        private const string UrlRoot = "zone-fx.azurewebsites.net";
        private const string ButtonAPressedText = "Button A Pressed";
        private const string ButtonANotPressedText = "Button A NOT Pressed";
        private const string TemperatureAlertText = "TemperatureAlert";
        private const string TemperatureNoAlertText = "No Alert";

        // url: https://zone-fx.azurewebsites.net/api/devkitgetdata/{devicename}?code=fF75zQJJSoyarmryXQlcj/smzcuYHM9IbCIPDBH3LvJIZbLWilIEpA==
        private readonly string _devkitGetDataUrl =
            $"https://{UrlRoot}/api/devkitgetdata/{DeviceId}?code=fF75zQJJSoyarmryXQlcj/smzcuYHM9IbCIPDBH3LvJIZbLWilIEpA==";

        // url: https://zone-fx.azurewebsites.net/api/devkitsetdata/{devicename}?code=1IROQHnPNk0Q01YQIis5eArJWr0O1mIMK41Szl1tUR6mseL2N9na6w==
        private readonly string _devkitSetDataUrl =
            $"https://{UrlRoot}/api/devkitsetdata/{DeviceId}?code=1IROQHnPNk0Q01YQIis5eArJWr0O1mIMK41Szl1tUR6mseL2N9na6w==";

        private DevkitData _currentData = new DevkitData();

        private async Task TimerCallback()
        {
            try
            {
                var value = await httpClient.GetAsync(_devkitGetDataUrl);
                var content = await value.Content.ReadAsStringAsync();
                _currentData = JsonConvert.DeserializeObject<DevkitData>(content);

                Humidity = $"{((int)Math.Round(_currentData.Humidity))}%";
                Temperature = _currentData.Temperature.ToString("N");
                ReportedInterval = _currentData.ReportedProperties.ContainsKey("interval")
                    ? Convert.ToInt32(_currentData.ReportedProperties["interval"])
                    : default(int);
                Pressure = _currentData.Pressure;
                //Interval = ReportedInterval;
                TemperatureAlert = _currentData.TemperatureAlert ? TemperatureAlertText : TemperatureNoAlertText;
                ButtonApressed = _currentData.ButtonApressed ? ButtonAPressedText : ButtonANotPressedText;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public DevKitPageViewModel()
        {
            _timer = new Timer(async o => await TimerCallback(), null, 0, 5000);
            SetDevkitDataCommand = new Command(async () => await SetDevkitData());
        }

        private string _temperature;

        public string Temperature
        {
            get => $"{_temperature}°C";
            set
            {
                if (Set(ref _temperature, value, nameof(Temperature)))
                {
                    RaisePropertyChanged(nameof(TemperatureColor));
                }
            }
        }

        public Color TemperatureColor => _currentData.Temperature > TemperatureThreshold ? Color.Red : Color.DarkBlue;

        private int _temperatureThreshold = 20;

        public int TemperatureThreshold
        {
            get => _temperatureThreshold;
            set
            {
                if (Set(ref _temperatureThreshold, value))
                {
                    RaisePropertyChanged(nameof(TemperatureThresholdText));
                    RaisePropertyChanged(nameof(TemperatureColor));
                }
            }
        }

        private bool _triggerRelay;

        public bool TriggerRelay
        {
            get => _triggerRelay;
            set => Set(ref _triggerRelay, value);
        }

        private int _interval;

        public int Interval
        {
            get => _interval;
            set => Set(ref _interval, value);
        }

        private async Task SetDevkitData()
        {
            var data = new
            {
                temperatureThreshold = TemperatureThreshold,
                triggerRelay = TriggerRelay,
                interval = Interval
            };
            var serialized = JsonConvert.SerializeObject(data);
            var content = new StringContent(serialized, Encoding.ASCII, "application/json");
            await httpClient.PostAsync(_devkitSetDataUrl, content);
        }

        public string TemperatureThresholdText
        {
            get => $"{_temperatureThreshold:N}°C";
        }

        public ICommand SetDevkitDataCommand { get; }


        private int _reportedInterval;

        public int ReportedInterval
        {
            get => _reportedInterval;
            set => Set(ref _reportedInterval, value);
        }

        private string _humidity;

        public string Humidity
        {
            get => _humidity;
            set => Set(ref _humidity, value);
        }

        private string _temperatureAlert;
        public string TemperatureAlert
        {
            get => _temperatureAlert;
            set
            {
                if (Set(ref _temperatureAlert, value))
                {
                    RaisePropertyChanged(nameof(TemperatureAlertColor));
                }
            }
        }

        public Color TemperatureAlertColor => TemperatureAlertText.Equals(TemperatureAlert) ? Color.Crimson : Color.CornflowerBlue;

        private string _buttonApressed;
        public string ButtonApressed
        {
            get => _buttonApressed;
            set
            {
                if (Set(ref _buttonApressed, value))
                {
                    RaisePropertyChanged(nameof(ButtonApressedColor));
                }
            }
        }

        public Color ButtonApressedColor => ButtonAPressedText.Equals(ButtonApressed) ? Color.Green : Color.Black;

        private double _pressure;
        public double Pressure
        {
            get => _pressure;
            set => Set(ref _pressure, value);
        }

    }
}