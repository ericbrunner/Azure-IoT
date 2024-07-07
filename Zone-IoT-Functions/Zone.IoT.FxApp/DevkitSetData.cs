using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Zone.IoT.FxApp
{
    public static class DevkitSetData
    {
        private static readonly string IothubCOnnectionString =
            Environment.GetEnvironmentVariable("IoThubConnectionString");

        static readonly RegistryManager IothubDeviceRegistryManager =
            RegistryManager.CreateFromConnectionString(IothubCOnnectionString);

        [FunctionName("DevkitSetData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function,
                "get",
                "post",
                Route = "devkitsetdata/{devicename}")]
            HttpRequest req,
            string devicename,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function DevkitSetData processed a request.");

            double? temperatureThreshold = null;
            bool? triggerRelay = null;
            int? interval = null;

            try
            {
                try
                {
                    temperatureThreshold = double.Parse(req.Query["temperatureThreshold"]);
                    triggerRelay = bool.Parse(req.Query["triggerRelay"]);
                    interval = int.Parse(req.Query["interval"]);
                }
                catch (Exception e)
                {
                    log.LogError(e, e.Message);
                }


                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);


                temperatureThreshold = temperatureThreshold ?? data?.temperatureThreshold;

                if (temperatureThreshold == null)
                    throw new ArgumentException("temperatureThreshold null", nameof(temperatureThreshold));

                triggerRelay = triggerRelay ?? data?.triggerRelay;

                if (triggerRelay == null)
                    throw new ArgumentException("triggerRelay null", nameof(triggerRelay));


                interval = interval ?? data?.interval;

                if (interval == null)
                    throw new ArgumentException("interval null", nameof(interval));
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                return new BadRequestObjectResult(e.Message);
            }


            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        temperatureThreshold,
                        triggerRelay,
                        interval
                    }
                }
            };

            var twin = await IothubDeviceRegistryManager.GetTwinAsync(devicename);
            await IothubDeviceRegistryManager.UpdateTwinAsync(
                twin.DeviceId,
                JsonConvert.SerializeObject(patch),
                twin.ETag);

            return new OkResult();
        }
    }
}