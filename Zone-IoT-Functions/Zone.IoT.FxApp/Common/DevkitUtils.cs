using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Zone.IoT.FxApp.Models;

namespace Zone.IoT.FxApp.Common
{
    public static class DevkitUtils
    {
        private static readonly string IothubCOnnectionString =
            Environment.GetEnvironmentVariable("IoThubConnectionString");

        static readonly RegistryManager IothubDeviceRegistryManager =
            RegistryManager.CreateFromConnectionString(IothubCOnnectionString);

        public static string ZoneAppDevConnectionString = "ZoneAppDEV";

        private static readonly SemaphoreSlim Locker =new SemaphoreSlim(1,1);
        public static async Task<DevkitData> GetDevkitDataAsync(EventData message, ILogger log)
        {
            DevkitData devkitData = new DevkitData();

            try
            {
                await Locker.WaitAsync();

                var jsonBody = Encoding.UTF8.GetString(message.Body.ToArray());
                log.LogInformation($"V2 C# IoT Hub trigger function processed a message: {jsonBody}");

                dynamic data = JsonConvert.DeserializeObject(jsonBody);
                int messageId = data?.messageId ?? 1;
                double temperature = data?.temperature ?? 0.0;
                double humidity = data?.humidity ?? 0.0;
                double pressure = data?.pressure ?? 0.0;

                message.SystemProperties.TryGetValue("iothub-enqueuedtime", out var iotHubEnqeueDateTime);
                message.SystemProperties.TryGetValue("iothub-connection-device-id", out var deviceId);
                
                
                message.Properties.TryGetValue("temperatureAlert", out var temperatureAlert);
                message.Properties.TryGetValue("buttonApressed", out var buttonApressed);
                message.Properties.TryGetValue("deviceShaked", out var deviceShaked);

                string deviceName = deviceId as string ?? "MXCHIP";

                var twin = await IothubDeviceRegistryManager.GetTwinAsync(deviceName);
                
                temperatureAlert ??= twin.Properties.Reported["temperatureAlert"];
                buttonApressed ??= twin.Properties.Reported["buttonApressed"];
                deviceShaked ??= twin.Properties.Reported["deviceShaked"];

                log.LogInformation("");
                log.LogInformation("");
                log.LogInformation($"message-id: {messageId}");
                log.LogInformation($"iothub-connection-device-id: {deviceId}");
                log.LogInformation($"iothub-enqueuedtime: {iotHubEnqeueDateTime}");
                log.LogInformation($"temperatureAlert: {temperatureAlert}");
                log.LogInformation($"buttonApressed: {buttonApressed}");
                log.LogInformation($"deviceShaked: {deviceShaked}");
                log.LogInformation($"Humiditiy: {humidity}");
                log.LogInformation($"Temperature: {temperature}");
                log.LogInformation($"Pressure: {pressure}");


                bool temoeratureAlertValue = temperatureAlert != null && Convert.ToBoolean(temperatureAlert);
                bool buttonApressedValue = buttonApressed != null && Convert.ToBoolean(buttonApressed);
                bool deviceShakedValue = deviceShaked != null && Convert.ToBoolean(deviceShaked);

#if DEBUG
                DateTime? iotHubEnqueueTimeValue = ((DateTime?) iotHubEnqeueDateTime)?.ToLocalTime();
#else
// When fx is hosted in Azure there local time is UTC , too. So I have to add 2 hours to get Vienna Local time
                DateTime? iotHubEnqueueTimeValue = ((DateTime?) iotHubEnqeueDateTime)?.AddHours(2);
#endif


                devkitData = new DevkitData
                {
                    Id = deviceId as string,
                    MessageId = messageId,
                    Temperature = temperature,
                    Humidity = humidity,
                    Pressure = pressure,
                    TemperatureAlert = temoeratureAlertValue,
                    ButtonApressed = buttonApressedValue,
                    DeviceShaked = deviceShakedValue,
                    IoTHubEnqueueTime = iotHubEnqueueTimeValue
                };

                
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
            }
            finally
            {
                Locker.Release();
            }

            return devkitData;
        }
    }
}