using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Zone.IoT.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static TelemetryClient TelemetryClient;

        public App()
        {
            #region Init Application Insights

            // ZoneIoTWinformsWpfHos Application Insights resource in my Azure Portal
            TelemetryConfiguration.Active.InstrumentationKey = "08a07468-1b70-4552-ae26-cd4f5c2de8cf";

            TelemetryClient = new TelemetryClient();

            TelemetryClient.Context.User.Id = Environment.UserName;
            TelemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            TelemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

            App.Current.Exit += async (sender, args) =>
            {
                if (TelemetryClient == null)
                    return;

                TelemetryClient.Flush();

                // Allow some time for flushing
                await Task.Delay(1000);
            };

            AppDomain.CurrentDomain.UnhandledException += (o, args) =>
            {
                TelemetryClient.TrackException(args.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (o, args) =>
            {
                TelemetryClient.TrackException(args.Exception);
                args.SetObserved();
            };

            
            Application.Current.DispatcherUnhandledException +=
                (sender, args) =>
                {
                    TelemetryClient.TrackException(args.Exception);
                    args.Handled = true;
                };

            TelemetryClient.TrackEvent("WPF App-Started.");

            #endregion
        }
    }
}