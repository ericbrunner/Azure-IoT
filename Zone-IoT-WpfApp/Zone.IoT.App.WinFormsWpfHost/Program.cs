using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Zone.IoT.App.WinFormsWpfHost
{
    static class Program
    {
        public static TelemetryClient TelemetryClient;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            #region Init Application Insights

            // ZoneIoTWinformsWpfHos Application Insights resource in my Azure Portal
            TelemetryConfiguration.Active.InstrumentationKey = "08a07468-1b70-4552-ae26-cd4f5c2de8cf";

            TelemetryClient = new TelemetryClient();

            TelemetryClient.Context.User.Id = Environment.UserName;
            TelemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            TelemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

            Application.ApplicationExit += async (sender, args) =>
            {
                if (TelemetryClient == null)
                    return;

                TelemetryClient.Flush();

                // Allow some time for flushing
                await Task.Delay(1000);
            };

            Application.ThreadException += (sender, args) => TelemetryClient.TrackException(args.Exception);

            AppDomain.CurrentDomain.UnhandledException += (o, args) =>
            {
                TelemetryClient.TrackException(args.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (o, args) =>
            {
                TelemetryClient.TrackException(args.Exception);
                args.SetObserved();
            };

            TelemetryClient.TrackEvent("WinForms App-Started.");
            #endregion


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}