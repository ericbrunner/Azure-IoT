using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Zone.IoT.FxApp.Models;

namespace FunctionCLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // The code provided will print ‘Hello World’ to the console.
            // Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.
            Console.WriteLine("Hello World!");

            const string devicename = "MXCHIP";

            HttpClient httpClient = new HttpClient();

            string testurl1 = "https://example.com";

            string testurl3 =
                $"https://YOUR-FUNCTION.azurewebsites.net/api/devkitgetdata/{devicename}?code=YOUR_FUNCTION_SAS_TOKEN";

            try
            {
                string json1 = await httpClient.GetStringAsync(testurl1);
                Console.WriteLine($"json1:{json1}");

                string json2 = await httpClient.GetStringAsync(testurl3);
                Console.WriteLine($"json3:{json2}");

                var devkitData = JsonConvert.DeserializeObject<DevkitData>(json2);
                Console.WriteLine($"${DateTime.UtcNow:O}-DevKitData: " +
                                  $"{nameof(devkitData.MessageId)}:{devkitData.MessageId} " +
                                  $"{nameof(devkitData.ButtonApressed)}:{devkitData.ButtonApressed}");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }


            Console.ReadKey();

            // Go to http://aka.ms/dotnet-get-started-console to continue learning how to build a console app! 
        }
    }
}