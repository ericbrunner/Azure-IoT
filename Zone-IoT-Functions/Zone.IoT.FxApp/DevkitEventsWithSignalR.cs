using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Zone.IoT.FxApp.Common;
using Zone.IoT.FxApp.Models;

namespace Zone.IoT.FxApp
{
    public static class DevkitEventsWithSignalR
    {
        //[Disable]
        [FunctionName("DevkitEventsWithSignalR")]
        public static async Task Run(
            [EventHubTrigger(
                "iothub-ehub-zone-iot-h-1282045-fdcb8f1f73",
                Connection = "EventHub",
#if LOCALHOST
                ConsumerGroup = "zone-iot-fx-DevkitEventsWithSignalR-localhost")]
#else
                ConsumerGroup = "zone-iot-function-DevkitEventsWithSignalR")]
#endif
            EventData[] events,
            [SignalR(HubName = "intercommHub")] IAsyncCollector<SignalRMessage> signalRMessages, ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    DevkitData devKitData = await DevkitUtils.GetDevkitDataAsync(eventData, log);
                    var devKitDataJson = JsonConvert.SerializeObject(devKitData);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {devKitDataJson}");
                    await signalRMessages.AddAsync(
                        new SignalRMessage
                        {
                            Target = "broadcastMessage",
                            Arguments = new object[] {"my-azure-function", devKitDataJson}
                        });
                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}