using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Zone.IoT.FxApp.Common;
using Zone.IoT.FxApp.Models;

namespace Zone.IoT.FxApp
{
    public static class DevkitEvents
    {
        //[Disable]
        [FunctionName("DevkitEvents")]
        public static async Task Run(
            [EventHubTrigger(
                "iothub-ehub-zone-iot-h-1282045-fdcb8f1f73",
                Connection = "EventHub",
#if LOCALHOST
                ConsumerGroup = "zone-iot-function-DevkitEvents-localhost")]
#else
                ConsumerGroup = "zone-iot-function-DevkitEvents")]
#endif
            EventData message,
            [Queue("zone-iot-hub-events",
                Connection = "StorageConnectionString")]
            IAsyncCollector<string> queueCollector,
            ILogger log)
        {
            DevkitData devKitData = await DevkitUtils.GetDevkitDataAsync(message, log);
            var devKitDataJson = JsonConvert.SerializeObject(devKitData);

            // Replace these two lines with your processing logic.
            log.LogInformation($"C# Event Hub trigger function processed a message: {devKitDataJson}");
            
            // Persist in Azure Storage Queue
            await queueCollector.AddAsync(devKitDataJson);

            // Persist in Azure SQL DB
            await UpdateDataAsync(log, devKitDataJson);
        }

        private static readonly SemaphoreSlim Locker = new SemaphoreSlim(1, 1);

        private static readonly string ZoneAppDevConnectionString =
            Environment.GetEnvironmentVariable(DevkitUtils.ZoneAppDevConnectionString);

        private static async Task UpdateDataAsync(ILogger log, string devKitDataJson)
        {
            await Locker.WaitAsync();

            try
            {
                await using var sqlconnection = new SqlConnection(ZoneAppDevConnectionString);
                sqlconnection.Open();

                var query =
                    "UPDATE [devkit].[DevKit_Temperature] SET [json] = @devkitData, [updatedAt] = @currentDateTime WHERE[sequenceId] = 1";


                await using var sqlCommand = new SqlCommand(query, sqlconnection);
                sqlCommand.Parameters.AddWithValue("@devkitData", devKitDataJson);
                sqlCommand.Parameters.AddWithValue("@currentDateTime", DateTime.Now);

                var updatedRecords = await sqlCommand.ExecuteNonQueryAsync();

                log.LogInformation($"{updatedRecords} rows where updated.");
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }
            finally
            {
                Locker.Release();
            }
        }
    }
}