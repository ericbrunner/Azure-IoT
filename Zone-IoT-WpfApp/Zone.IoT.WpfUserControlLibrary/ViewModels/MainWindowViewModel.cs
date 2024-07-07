using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;
using Zone.IoT.App.Models;
using Zone.IoT.App.ViewModels.Base;
using Zone.IoT.App.ViewModels.Commands;
using Zone.IoT.WpfUserControlLibrary.Events;

namespace Zone.IoT.App.ViewModels
{
    internal class MainWindowViewModel : BaseViewModel
    {
        internal event Events.SendButtonClickedDelegate SendButtonClicked;
        private readonly DispatcherTimer _timer;
        private readonly HttpClient httpClient;


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
                HttpResponseMessage value = await httpClient.GetAsync(_devkitGetDataUrl);
                string content = await value.Content.ReadAsStringAsync();
                _currentData = JsonConvert.DeserializeObject<DevkitData>(content);

                Humidity = $"{((int) Math.Round(_currentData.Humidity))} %";
                Pressure = $"{((int) Math.Round(_currentData.Pressure))} mBar";

                Temperature = _currentData.Temperature.ToString("N");
                ReportedInterval = _currentData.ReportedProperties.ContainsKey("interval")
                    ? Convert.ToInt32(_currentData.ReportedProperties["interval"])
                    : default(int);
                //Interval = ReportedInterval;
                TemperatureAlert = _currentData.TemperatureAlert ? TemperatureAlertText : TemperatureNoAlertText;
                ButtonApressed = _currentData.ButtonApressed ? ButtonAPressedText : ButtonANotPressedText;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        private string _pressure;
        public string Pressure
        {
            get => _pressure;
            set
            {
                if (Set(ref _pressure, value))
                {
                    OnPropertyChanged(nameof(PressureInHectoPascal));
                };
            }
        }

        public string PressureInHectoPascal => $"{((int)Math.Round(_currentData.Pressure))} hPa";

        private string _buttonApressed;

        public string ButtonApressed
        {
            get => _buttonApressed;
            set
            {
                if (Set(ref _buttonApressed, value))
                {
                    OnPropertyChanged(nameof(ButtonAColor));
                }
            }
        }

        public Brush ButtonAColor => _currentData.ButtonApressed
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.Black);

        private string _temperatureAlert;

        public string TemperatureAlert
        {
            get => _temperatureAlert;
            set
            {
                if (Set(ref _temperatureAlert, value))
                {
                    OnPropertyChanged(nameof(TemperatureAlertColor));
                }
            }
        }

        public Brush TemperatureAlertColor => _currentData.TemperatureAlert
            ? new SolidColorBrush(Colors.Crimson)
            : new SolidColorBrush(Colors.DarkBlue);

        private int _reportedInterval;

        public int ReportedInterval
        {
            get => _reportedInterval;
            set => Set(ref _reportedInterval, value);
        }

        private readonly DevkitCommand _devkitsetDataCommand;
        private readonly DevkitCommand _relayTriggerCommand;


        public MainWindowViewModel()
        {
            Temperature = "init...";
            Humidity = "init...";
            Pressure = "init...";
            SendButtonColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF391AC5");
            httpClient = new HttpClient();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += _timer_Tick;
            _timer.Start();

            _devkitsetDataCommand =
                new DevkitCommand(async (commandParameter) => await SetDevkitData(commandParameter));


            _relayTriggerCommand = new DevkitCommand((commandParameter) => TriggerRelay = (bool) commandParameter);
        }

        private async void _timer_Tick(object sender, EventArgs e)
        {
            await TimerCallback();
        }

        private string _temperature;

        public string Temperature
        {
            get => $"{_temperature}°C";
            set
            {
                if (Set(ref _temperature, value, nameof(Temperature)))
                {
                    //OnPropertyChanged(nameof(TemperatureColor));
                }
            }
        }

        //public Color TemperatureColor => _currentData.Temperature > TemperatureThreshold ? Color.Red : Color.DarkBlue;

        private string _humidity;

        public string Humidity
        {
            get => _humidity;
            set => Set(ref _humidity, value);
        }

        public bool TriggerRelay { get; set; }

        public ICommand SetDevkitDataCommand => _devkitsetDataCommand;
        public ICommand RelayTriggerCommand => _relayTriggerCommand;

        private async Task SetDevkitData(object commandParameter)
        {
            try
            {
                OnSendButtonClicked(this, new WpfEventArgs{EventName = nameof(SetDevkitData)});
                SendStatus = "Status: Begin C2D sending ...";

                var data = new
                {
                    temperatureThreshold = TemperatureThreshold,
                    triggerRelay = TriggerRelay,
                    interval = Interval
                };
                string serialized = JsonConvert.SerializeObject(data);
                StringContent content = new StringContent(serialized, Encoding.ASCII, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(_devkitSetDataUrl, content);

                SendStatus = response.IsSuccessStatusCode
                    ? "Status: Success"
                    : $"Status: Http Error {(int)response.StatusCode}-{response.ReasonPhrase}";
            }
            catch (Exception e)
            {
                SendStatus = $"Error: {e.Message}";
            }
        }

        private string _sendStatus;

        public string SendStatus
        {
            get => _sendStatus;
            set => Set(ref _sendStatus, value);
        }

        public int Interval { get; set; }

        public double TemperatureThreshold { get; set; }

        protected virtual void OnSendButtonClicked(object sender, WpfEventArgs e)
        {
            SendButtonClicked?.Invoke(sender, e);
        }

        private SolidColorBrush _sendButtonColor;
        public SolidColorBrush SendButtonColor
        {
            get => _sendButtonColor;
            set => Set(ref _sendButtonColor, value);
        }
    }
}