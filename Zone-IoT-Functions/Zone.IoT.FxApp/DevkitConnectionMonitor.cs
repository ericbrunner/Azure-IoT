using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Zone.IoT.FxApp
{
    public static class DevkitConnectionMonitor
    {
        [Disable]
        [FunctionName("DevkitConnectionMonitor")]
        public static void Run([BlobTrigger("insights-logs-connections/{name}", Connection = "StorageConnectionString")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
}
