using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using MvvmHelpers;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace Zone_IoT_MxChipApp.ViewModels;

public class MainPageViewModel : BaseViewModel, IAsyncDisposable
{
    private readonly ILogger<MainPageViewModel> _logger;
    private DeviceClient _deviceClient;
    private RegistryManager _registryManager;

    private readonly Task _deviceTwinRunTask;

    // The device connection string to authenticate the device with your IoT hub.
    // Using the Azure CLI:
    // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDevice --output table
    private const string DeviceId = "MXCHIP";
    private const string IotHubDeviceConnectionString =
        $"HostName=YOUR_IOT_HUB.azure-devices.net;DeviceId={DeviceId};SharedAccessKey=YOUR_SAS_TOKEN";

    private const string IotHubConnectionString =
        "HostName=YOUR_IOT_HUB.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=YOUR_SAS_TOKEN";

    public MainPageViewModel(IServiceProvider serviceProvider, int millisecondsInterval)
    {
        _logger = serviceProvider.GetService<ILogger<MainPageViewModel>>();
        InitIotDevice();

        _cts = new CancellationTokenSource();
        _deviceTwinRunTask = DeviceTwinHandler(millisecondsInterval, _cts.Token);
    }

    private async Task DeviceTwinHandler(int millisecondsInterval, CancellationToken? cancellationToken = default)
    {
        try
        {
            while (true)
            {
                cancellationToken?.ThrowIfCancellationRequested();


                var deviceTwin = await _registryManager.GetTwinAsync(DeviceId);

                //var deviceTwin = await _deviceClient.GetTwinAsync(cancellationToken ?? default); //TODO that call blocks when read from more than one application


                try
                {
                    
                    // CLOUD 2 DEVICE == Desired Property
                    string triggerRelayRaw = deviceTwin.Properties.Desired[TriggerRelay].ToString();
                    System.Diagnostics.Debug.Write($"desired trigger-value: {triggerRelayRaw} ");
                    LedState = bool.Parse(triggerRelayRaw);
                    System.Diagnostics.Debug.Write($"{nameof(LedState)}: {LedState} \n");
                    

                    // DEVICE 2 CLOUD == Reported Property
                    string buttonApressedRaw = deviceTwin.Properties.Reported[ButtonApressed].ToString();
                    System.Diagnostics.Debug.Write($"reported button-a-value: {buttonApressedRaw} ");
                    ButtonAState = bool.Parse(buttonApressedRaw);
                    System.Diagnostics.Debug.Write($"{nameof(ButtonAState)}: {ButtonAState} \n");
                    
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"\"{TriggerRelay}\" not convertable to boolean. Error: {e.Message}");
                }


                await Task.Delay(millisecondsInterval);
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e);
        }

    }


    private const string TriggerRelay = "triggerRelay";
    private const string ButtonApressed = "buttonApressed";
    private void InitIotDevice()
    {
        _deviceClient = DeviceClient.CreateFromConnectionString(IotHubDeviceConnectionString, TransportType.Mqtt);

        _registryManager = RegistryManager.CreateFromConnectionString(IotHubConnectionString);
    }


    private bool _ledState;
    private readonly CancellationTokenSource _cts;

    public bool LedState
    {
        get => _ledState;
        set => SetProperty(ref _ledState, value);
    }

    private bool _buttonAState;
    public bool ButtonAState
    {
        get => _buttonAState;
        set => SetProperty(ref _buttonAState, value);
    }


    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts.Cancel();

            await _deviceTwinRunTask;
        }
        catch (OperationCanceledException oce)
        {
            System.Diagnostics.Debug.WriteLine("IoT Device ViewModel gracefully shut down.");
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"IoT Device ViewModel shutdown error: {e.Message}");
        }
    }
}