using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Zone.IoT.FxApp.Models
{
    public class DevkitData
    {
        [JsonProperty("id")] public string Id { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public bool TemperatureAlert { get; set; }
        public bool ButtonApressed { get; set; }
        public bool DeviceShaked { get; set; }
        public DateTime? IoTHubEnqueueTime { get; set; }
        public int MessageId { get; set; }
        public bool IsOnline { get;set;}

        public Dictionary<string, object> ReportedProperties = new Dictionary<string, object>();
    }
}