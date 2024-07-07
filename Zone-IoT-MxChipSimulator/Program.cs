using System.IO.Enumeration;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Zone_IoT_MxChipSimulator
{
    public sealed class SimulatedDevice
    {
        private static DeviceClient _deviceClient;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDevice --output table
        private const string ConnectionString =
            "HostName=YOUR_IOT_HUB.azure-devices.net;DeviceId=MXCHIP;SharedAccessKey=YOUR_SAS_TOKEN";

        private static int _telemetryInterval = 20; // Seconds

        // Handle the direct method call
        private static Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            // Check the payload is a single integer value
            if (Int32.TryParse(data.Replace("\"", ""), out _telemetryInterval))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Telemetry interval set to {0} seconds", data);
                Console.ResetColor();

                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }

        // Async method to send simulated telemetry
        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken? cancellationToken = default)
        {
            // Initial telemetry values
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();

            int messageId = 0;
            while (true)
            {
                cancellationToken?.ThrowIfCancellationRequested();

                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                // Create JSON message
                var telemetryDataPoint = new
                {
                    messageId = messageId++,
                    temperature = currentTemperature,
                    humidity = currentHumidity,
                    pressure = 1.02
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                #region Read/Write Device Twin

                Console.WriteLine("Retrieving device twin ...");
                var twin = await _deviceClient.GetTwinAsync();

                // Read Device Twin (DESIRED PROPERTIES)
                Console.WriteLine($"Desired device twin properties: {twin.Properties.Desired.ToJson()}");

                // Write Device Twin (REPORTED PROPERTIES)
                var reportedProperties = new TwinCollection();
                reportedProperties["deviceShaked"] = messageId % 10 == 0;
                reportedProperties["buttonApressed"] = messageId % 2 == 0;

                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
                Console.WriteLine($"Reported device twin properties: {twin.Properties.Reported.ToJson()} ");

                #endregion

                // Send the telemetry message
                await _deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(_telemetryInterval * 1000);
            }
        }

        private static async Task Main(string[] args)
        {
            Console.WriteLine("IoT Hub - Simulated MXCHIP device. Hit any key to exit.\n");

            bool stopped = false;
            try
            {
                // Connect to the IoT hub using the MQTT protocol
                _deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, TransportType.Mqtt);


                CancellationTokenSource cts = new CancellationTokenSource();

                ReceiveMessageCallback messageHandler = MessageHandler;
                await _deviceClient.SetReceiveMessageHandlerAsync(messageHandler, _deviceClient, cts.Token);

                // Create a handler for the direct method call
                await _deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null,
                    cts.Token);
                Task runTask = SendDeviceToCloudMessagesAsync(cts.Token);


                Console.ReadKey(intercept: true);

                _ = Task.Run(async () =>
                {
                    while (!stopped)
                    {
                        Console.Write('.');
                        await Task.Delay(500);
                    }
                });

                cts.Cancel();

                Console.WriteLine("Iot Device shutdown process initiated ....");
                await runTask;
                stopped = true;
            }
            catch (OperationCanceledException oce)
            {
                stopped=true;
                Console.WriteLine();
                Console.WriteLine("IoT Device gracefully shut down.");
            }
            catch (Exception e)
            {
                stopped = true;
                Console.WriteLine(e);
            }
        }


        private static async Task MessageHandler(Message message, object usercontext)
        {
            string messageData = Encoding.UTF8.GetString(message.GetBytes());

            var formattedMessage = new StringBuilder($"Received message: [{messageData}] + {Environment.NewLine}");


            foreach (var messageProperty in message.Properties)
            {
                formattedMessage.Append($"\tProperty: {messageProperty.Key}={messageProperty.Value}");
            }

            await _deviceClient.CompleteAsync(message);
            Console.WriteLine($"{DateTime.Now}> Completed C2D message with Id={message.MessageId} " +
                              $"{Environment.NewLine}" +
                              $"{formattedMessage}");
        }
    }
}