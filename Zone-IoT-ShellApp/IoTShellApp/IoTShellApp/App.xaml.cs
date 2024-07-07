using System;
using Xamarin.Forms;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Distribute;
using Microsoft.AppCenter.Push;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features.Authentication;
using IoTShellApp.Identity;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IoTShellApp
{
    public partial class App : Application
    {

        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjA2OTc5QDMxMzcyZTM0MmUzMGxTMGJJekM4MEl3eDh3cElLNmFuWS9JaHpRc1haclhDVjZvVUo2bWRmL0U9");

            InitializeComponent();
            MainPage = new AppShell();
        }

        // UIParent used by Android version of the app
        public static object AuthUIParent = null;

        // Microsoft Authentication client for native/mobile apps
        public static IPublicClientApplication PCA;

        // Microsoft Graph permissions used by app
        private readonly string[] Scopes = OAuthSettings.Scopes.Split(' ');

        protected override async void OnStart()
        {
            // This should come before AppCenter.Start() is called
            // Avoid duplicate event registration:
            if (!AppCenter.Configured)
            {
                Push.PushNotificationReceived += (sender, e) =>
                {
                    try
                    {
                        Xamarin.Essentials.Vibration.Vibrate();
                    }
                    catch (Exception ex)
                    {
                        Crashes.TrackError(ex);
                        
                    }


                    // Add the notification message and title to the message
                    var summary = $"Push notification received:" +
                                        $"\n\tNotification title: {e.Title}" +
                                        $"\n\tMessage: {e.Message}";

                    // If there is custom data associated with the notification,
                    // print the entries
                    if (e.CustomData != null)
                    {
                        summary += "\n\tCustom data:\n";
                        foreach (var key in e.CustomData.Keys)
                        {
                            summary += $"\t\t{key} : {e.CustomData[key]}\n";
                        }
                    }

                    // Send the notification summary to debug output
                    System.Diagnostics.Debug.WriteLine(summary);
                };
            }

            AppCenter.Start("android=1a9c9765-c683-450e-a3c5-5de174ea1727;" +
                  "uwp={Your UWP App secret here};" +
                  "ios={Your iOS App secret here}",
                  typeof(Analytics), typeof(Crashes), typeof(Distribute), typeof(Push));

            #region MSAL Init & SignIn

            var builder = PublicClientApplicationBuilder
                .Create(OAuthSettings.ApplicationId)
                .WithRedirectUri(OAuthSettings.RedirectUri);

            if (!string.IsNullOrWhiteSpace(OAuthSettings.TenantId))
            {
                builder.WithTenantId(OAuthSettings.TenantId);
            }

            PCA = builder.Build();

            await SignIn();
            #endregion

        }

        public async Task SignIn()
        {
            string? _accessToken  =null;

            // <GetTokenSnippet>
            // First, attempt silent sign in
            // If the user's information is already in the app's cache,
            // they won't have to sign in again.
            try
            {
                var accounts = await PCA.GetAccountsAsync();

                var silentAuthResult = await PCA
                    .AcquireTokenSilent(Scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();

                Debug.WriteLine("User already signed in.");
                Debug.WriteLine($"Successful silent authentication for: {silentAuthResult.Account.Username}");
                Debug.WriteLine($"Access token: {silentAuthResult.AccessToken}");

                _accessToken = silentAuthResult.AccessToken;
            }
            catch (MsalUiRequiredException msalEx)
            {
                // This exception is thrown when an interactive sign-in is required.
                Debug.WriteLine("Silent token request failed, user needs to sign-in: " + msalEx.Message);
                // Prompt the user to sign-in
                var interactiveRequest = PCA.AcquireTokenInteractive(Scopes);

                if (AuthUIParent != null)
                {
                    interactiveRequest = interactiveRequest
                        .WithParentActivityOrWindow(AuthUIParent);
                }

                var interactiveAuthResult = await interactiveRequest.ExecuteAsync();
                Debug.WriteLine($"Successful interactive authentication for: {interactiveAuthResult.Account.Username}");
                Debug.WriteLine($"Access token: {interactiveAuthResult.AccessToken}");

                _accessToken = interactiveAuthResult.AccessToken;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Authentication failed. See exception messsage for more details: " + ex.Message);
            }


            #region Test - Call Microsoft Identiy protected Weather API 

            if (!string.IsNullOrEmpty(_accessToken))
            {
                using var client = new HttpClient();

                var message = new HttpRequestMessage(HttpMethod.Get, "https://meteoapi.azurewebsites.net/WeatherForecast");
                message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                var weatherApiRequest = await client.SendAsync(message);

                if (weatherApiRequest.IsSuccessStatusCode)
                {
                    var json = await weatherApiRequest.Content.ReadAsStringAsync();

                    Debug.WriteLine($"GET api/weather: {json}");
                }
            }
            #endregion
        }


        public async Task SignOut()
        {
            var accounts = await PCA.GetAccountsAsync();
            while (accounts.Any())
            {
                // Remove the account info from the cache
                await PCA.RemoveAsync(accounts.First());
                accounts = await PCA.GetAccountsAsync();
            }
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
