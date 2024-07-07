using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Zone.IoT.FxApp.Common;
using Zone.IoT.FxApp.Models;

namespace Zone.IoT.FxApp
{
    public static class DevKitGetData
    {
        private static readonly string IothubCOnnectionString =
            Environment.GetEnvironmentVariable("IoThubConnectionString");

        private static readonly RegistryManager IothubDeviceRegistryManager =
            RegistryManager.CreateFromConnectionString(IothubCOnnectionString);

        private static readonly CloudQueue Queue;

        private static readonly string ZoneAppDevConnectionString =
            Environment.GetEnvironmentVariable(DevkitUtils.ZoneAppDevConnectionString);

        private static DevkitData _lastDevKitData;

        static DevKitGetData()
        {
            _lastDevKitData =new DevkitData() {MessageId = -1};
            
            string queueCns = Environment.GetEnvironmentVariable("StorageConnectionString");

            // Retrieve storage account from connection string

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(queueCns);

            // Create the queue client
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue
            Queue = queueClient.GetQueueReference("zone-iot-hub-events");
        }

        private static async Task<string> GetDataAsync(ILogger log)
        {
            string result = string.Empty;

            try
            {
                await using var sqlconnection = new SqlConnection(ZoneAppDevConnectionString);
                sqlconnection.Open();

                var query = "SELECT TOP(1) * FROM [devkit].[DevKit_Temperature] WHERE sequenceId = 1";

                await using var sqlCommand = new SqlCommand(query, sqlconnection);
                SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();

                if (sqlDataReader.HasRows)
                {
                    await sqlDataReader.ReadAsync();
                    int columnIndex = sqlDataReader.GetOrdinal("json");
                    string devkitData = sqlDataReader.GetString(columnIndex);

                    result = devkitData;
                }
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }

            return result;
        }



        [FunctionName("DevkitGetData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "devkitgetdata/{devicename}")]
            HttpRequest req,
            string devicename,
            ILogger log)
        {
            string devkitData;

            try
            {
                // Peek at the next message
                //CloudQueueMessage retrievedMessage = await Queue.GetMessageAsync();
                //devkitData = retrievedMessage.AsString;

                devkitData = await GetDataAsync(log);

                // Display message.
                log.LogInformation($"device-id: {devicename}");
                log.LogInformation($"de-queued devkitData: {devkitData}");

                //await Queue.DeleteMessageAsync(retrievedMessage);
                var devkitDataMaterialized = JsonConvert.DeserializeObject<DevkitData>(devkitData) ?? _lastDevKitData;


                // prevent unneccessary device twin reads if the retrieved message is the same as the last one.
                var isDevKitOnline = _lastDevKitData.MessageId != devkitDataMaterialized.MessageId;
                var initialTwinRead = _lastDevKitData.MessageId == -1;
                log.LogInformation($"Devkit Online: {isDevKitOnline}");

                devkitDataMaterialized.IsOnline = isDevKitOnline;

                if (initialTwinRead || isDevKitOnline)
                {
                    log.LogInformation($"DEVICE-TWIN -- READ on new MessageId {devkitDataMaterialized.MessageId}");

                    
                    Twin? twin = null;
                    try
                    {
                        // Query Device-Twin for last Device State
                        twin = await IothubDeviceRegistryManager.GetTwinAsync(devicename, new CancellationTokenSource(1000).Token);
                    }
                    catch (Exception e)
                    {
                        log.LogError($"DeviceTwin Error: {e.Message}");
                    }

                    if (twin != null)
                    {
                        if (twin.Properties.Reported.Contains("interval"))
                        {
                            devkitDataMaterialized.ReportedProperties["interval"] = twin.Properties.Reported["interval"];
                        }

                        if (twin.Properties.Reported.Contains("wifiSSID"))
                        {
                            devkitDataMaterialized.ReportedProperties["wifiSSID"] = twin.Properties.Reported["wifiSSID"];
                        }
                    }
                }
                else
                {
                    log.LogInformation($"DEVICE-TWIN -- NO READ on same MessageId {devkitDataMaterialized.MessageId}");
                }

                _lastDevKitData = devkitDataMaterialized;

                devkitData = JsonConvert.SerializeObject(devkitDataMaterialized);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);


                var error = new HttpResponseMessage
                {
                    Content = new StringContent(e.Message),
                };

                return new BadRequestObjectResult(error);
            }


            return new OkObjectResult(devkitData);
        }
    }
}