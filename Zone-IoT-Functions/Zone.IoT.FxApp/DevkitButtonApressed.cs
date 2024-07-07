using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Zone.IoT.FxApp
{
    public static class DevkitButtonApressed
    {
        // message rule: buttonApressed == true

        [Disable]
        [FunctionName("DevkitButtonApressed")]
        public static void Run([BlobTrigger("buttonapressed/{name}", Connection = "StorageConnectionString")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            // TODO: Parse ARVO formatted data back in C# .NET object instance

            // TODO: EF SQL SERVER, 


        }
    }
}
