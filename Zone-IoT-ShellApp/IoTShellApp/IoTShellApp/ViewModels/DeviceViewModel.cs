using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Zone.IoT.Models;

namespace IoTShellApp.ViewModels
{
    class DeviceViewModel : BaseViewModel
    {
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

                return new HttpClient();
            }
        }

        private const string DeviceId = "MXCHIP";
        private const string UrlRoot = "YOUR-FUNCTION.azurewebsites.net";
        private const string ButtonAPressedText = "Button A Pressed";
        private const string ButtonANotPressedText = "Button A NOT Pressed";
        private const string TemperatureAlertText = "TemperatureAlert";
        private const string TemperatureNoAlertText = "No Alert";
        private const string DeviceShakedText = "DEVICE SHAKED!!!";
        private const string DeviceNotShakedText = "";

        private readonly string _devkitGetDataUrl =
            $"https://{UrlRoot}/api/devkitgetdata/{DeviceId}?code=YOUR_FUNCTION_SAS_TOKEN";

        private readonly string _devkitSetDataUrl =
            $"https://{UrlRoot}/api/devkitsetdata/{DeviceId}?code=YOUR_FUNCTION_SAS_TOKEN";

        private DevkitData _currentData = new DevkitData();
        private readonly SemaphoreSlim syncTimerCallback = new SemaphoreSlim(1, 1);
        private async Task TimerCallback()
        {
            try
            {
                await syncTimerCallback.WaitAsync();

                HttpResponseMessage response = await httpClient.GetAsync(_devkitGetDataUrl);

                if (response.IsSuccessStatusCode)
                {
                    ErrorMessage = null;
                    var content = await response.Content.ReadAsStringAsync();
                    _currentData = JsonConvert.DeserializeObject<DevkitData>(content);

                    MessageId = _currentData.MessageId;
                    IoTHubEnqueueTime = _currentData.IoTHubEnqueueTime ?? DateTime.Now;
                    Humidity = $"{((int)Math.Round(_currentData.Humidity * 0.9))}%";
                    Temperature = (_currentData.Temperature * 0.9).ToString("N");
                    ReportedInterval = _currentData.ReportedProperties.ContainsKey("interval")
                        ? Convert.ToInt32(_currentData.ReportedProperties["interval"])
                        : default(int);
                    Pressure = _currentData.Pressure;
                    //Interval = ReportedInterval;
                    TemperatureAlert = _currentData.TemperatureAlert ? TemperatureAlertText : TemperatureNoAlertText;
                    ButtonApressed = _currentData.ButtonApressed ? ButtonAPressedText : ButtonANotPressedText;
                    DeviceShaked = _currentData.DeviceShaked ? DeviceShakedText : DeviceNotShakedText;
                    IsOnline = _currentData.IsOnline;


                    if (DeviceShakedText.Equals(DeviceShaked))
                    {
                        Vibration.Vibrate();
                    }
                }
                else
                {
                    ErrorMessage = await response.Content.ReadAsStringAsync();
                }

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                ErrorMessage = e.Message;
            }
            finally
            {
                syncTimerCallback.Release();
            }
        }

        private string _errorMessage;

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public DeviceViewModel()
        {
            _timer = new Timer(async o => await TimerCallback(), null, 0, 3000);
            SetDevkitDataCommand = new Command(async () => await SetDevkitData());
        }

        private string _temperature;

        public string Temperature
        {
            get => $"{_temperature}°C";
            set
            {
                if (SetProperty(ref _temperature, value, nameof(Temperature)))
                {
                    OnPropertyChanged(nameof(TemperatureColor));
                }
            }
        }

        public Color TemperatureColor => _currentData.Temperature > TemperatureThreshold ? Color.Red : Color.Green;

        private int _temperatureThreshold = 20;

        public int TemperatureThreshold
        {
            get => _temperatureThreshold;
            set
            {
                if (SetProperty(ref _temperatureThreshold, value))
                {
                    OnPropertyChanged(nameof(TemperatureThresholdText));
                    OnPropertyChanged(nameof(TemperatureColor));
                }
            }
        }

        private bool _triggerRelay;

        public bool TriggerRelay
        {
            get => _triggerRelay;
            set => SetProperty(ref _triggerRelay, value);
        }

        private int _interval;

        public int Interval
        {
            get => _interval;
            set => SetProperty(ref _interval, value);
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
            set => SetProperty(ref _reportedInterval, value);
        }

        private string _humidity;

        public string Humidity
        {
            get => _humidity;
            set => SetProperty(ref _humidity, value);
        }

        private string _temperatureAlert;

        public string TemperatureAlert
        {
            get => _temperatureAlert;
            set
            {
                if (SetProperty(ref _temperatureAlert, value))
                {
                    OnPropertyChanged(nameof(TemperatureAlertColor));
                }
            }
        }

        public Color TemperatureAlertColor =>
            TemperatureAlertText.Equals(TemperatureAlert) ? Color.Crimson : Color.CornflowerBlue;

        private string _buttonApressed;

        public string ButtonApressed
        {
            get => _buttonApressed;
            set
            {
                if (SetProperty(ref _buttonApressed, value))
                {
                    OnPropertyChanged(nameof(ButtonApressedColor));
                }
            }
        }

        public Color ButtonApressedColor => ButtonAPressedText.Equals(ButtonApressed) ? Color.Green : Color.Gray;

        private string _deviceShaked;

        public string DeviceShaked
        {
            get => _deviceShaked;
            set
            {
                if (SetProperty(ref _deviceShaked, value))
                {
                    OnPropertyChanged(nameof(DeviceShakedColor));
                }
            }
        }

        public Color DeviceShakedColor => DeviceNotShakedText.Equals(DeviceShaked) ? Color.RoyalBlue : Color.Aquamarine;

        private double _pressure;

        public double Pressure
        {
            get => _pressure;
            set => SetProperty(ref _pressure, value);
        }

        private int _messageId;

        public int MessageId
        {
            get => _messageId;
            set => SetProperty(ref _messageId, value);
        }

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            private set
            {
                if (SetProperty(ref _isOnline, value))
                {
                    OnPropertyChanged(nameof(IsOnlineFormatted));
                }
            }
        }

        public FormattedString IsOnlineFormatted
        {
            get
            {
                FormattedString result = new FormattedString
                {

                    Spans =
                    {
                        new Span
                        {
                            Text = IsOnline ? "Got new Data" : "waiting for new data...",
                            FontAttributes = FontAttributes.Bold,
                            TextColor = IsOnline ? Color.Green : Color.DarkGray
                        }
                    }
                };

                return result;
            }

        }


        private DateTime _iotHubEnqeueTime;

        public DateTime IoTHubEnqueueTime
        {
            get => _iotHubEnqeueTime;
            set => SetProperty(ref _iotHubEnqeueTime, value);
        }
    }
}
