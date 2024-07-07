// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// To get started please visit https://microsoft.github.io/azure-iot-developer-kit/docs/projects/connect-iot-hub?utm_source=ArduinoExtension&utm_medium=ReleaseNote&utm_campaign=VSCode
#include "AZ3166WiFi.h"
#include "AzureIotHub.h"
#include "DevKitMQTTClient.h"
#include "parson.h"

#include "config.h"
#include "utility.h"
#include "SystemTickCounter.h"


static bool hasWifi = false;
int messageCount = 1;
static bool messageSending = true;
static uint64_t send_interval_ms;
volatile bool buttonApressed = false;

//////////////////////////////////////////////////////////////////////////////////////////////////////////
// Utilities
static void InitWifi()
{
  Screen.print(2, "Connecting...");

  if (WiFi.begin() == WL_CONNECTED)
  {
    IPAddress ip = WiFi.localIP();
    Screen.print(1, ip.get_address());
    hasWifi = true;
    Screen.print(2, "Running... \r\n");
  }
  else
  {
    hasWifi = false;
    Screen.print(1, "No Wi-Fi\r\n ");
  }
}

static void SendConfirmationCallback(IOTHUB_CLIENT_CONFIRMATION_RESULT result)
{
  if (result == IOTHUB_CLIENT_CONFIRMATION_OK)
  {
    blinkSendConfirmation();
  }
}

static void MessageCallback(const char *payLoad, int size)
{
  blinkLED();
  Screen.print(1, payLoad, true);
}

static void DeviceTwinCallback(DEVICE_TWIN_UPDATE_STATE updateState, const unsigned char *payLoad, int size)
{
  // char *temp = (char *)malloc(size + 1);
  // if (temp == NULL)
  // {
  //   return;
  // }
  // memcpy(temp, payLoad, size);
  // temp[size] = '\0';
  // parseTwinMessage(updateState, temp);
  // free(temp);

  char *message = (char *)malloc(size + 1);

  if (message == NULL)
  {
    return;
  }

  memcpy(message, payLoad, size);
  message[size] = '\0';

  JSON_Value *root_value = json_parse_string(message);
  JSON_Object *root_object = json_value_get_object(root_value);

  double val = 0;

  if (updateState == DEVICE_TWIN_UPDATE_COMPLETE)
  {
    JSON_Object *desired_object = json_object_get_object(root_object, "desired");

    if (desired_object != NULL)
    {
      if (json_object_has_value(desired_object, "temperatureThreshold"))
      {
        temperatureThreshold = json_object_get_number(desired_object, "temperatureThreshold");
      }

      if (json_object_has_value(desired_object, "triggerRelay"))
      {
        triggerRelay = json_object_get_boolean(desired_object, "triggerRelay");
      }

      if (json_object_has_value(desired_object, "interval"))
      {
        val = json_object_get_number(desired_object, "interval");
      }
    }
  }
  else
  {
    if (json_object_has_value(root_object, "temperatureThreshold"))
    {
      temperatureThreshold = json_object_get_number(root_object, "temperatureThreshold");
    }

    if (json_object_has_value(root_object, "triggerRelay"))
    {
      triggerRelay = json_object_get_boolean(root_object, "triggerRelay");
    }

    if (json_object_has_value(root_object, "interval"))
    {
      val = json_object_get_number(root_object, "interval");
    }
  }

  if (val > 500)
  {
    interval = (int)val;
    LogInfo(">>>Device twin updated: set interval to %d", interval);
  }

  if (triggerRelay) {
    userLEDOn();
    digitalWrite(PA_5, HIGH);
    
    delay(1000);

    userLEDOff();
    digitalWrite(PA_5, LOW);
  }
  
  json_value_free(root_value);
  free(message);
}

static int DeviceMethodCallback(const char *methodName, const unsigned char *payload, int size, unsigned char **response, int *response_size)
{
  LogInfo("Try to invoke method %s", methodName);
  const char *responseMessage = "\"Successfully invoke device method\"";
  int result = 200;

  if (strcmp(methodName, "start") == 0)
  {
    LogInfo("Start sending temperature and humidity data");
    messageSending = true;
  }
  else if (strcmp(methodName, "stop") == 0)
  {
    LogInfo("Stop sending temperature and humidity data");
    messageSending = false;
  }
  else if (strcmp(methodName, "ledon") == 0)
  {
    userLEDOn();
  }
  else if (strcmp(methodName, "ledoff") == 0)
  {
    userLEDOff();
  }
  else
  {
    LogInfo("No method %s found", methodName);
    responseMessage = "\"No method found\"";
    result = 404;
  }

  *response_size = strlen(responseMessage) + 1;
  *response = (unsigned char *)strdup(responseMessage);

  return result;
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////
// Arduino sketch
void setup()
{
  Screen.init();
  Screen.print(0, "Eric IoT Kit");
  Screen.print(2, "Initializing...");

  Screen.print(3, " > Serial");
  Serial.begin(115200);

  // Initialize the WiFi module
  Screen.print(3, " > WiFi");
  hasWifi = false;
  InitWifi();
  if (!hasWifi)
  {
    return;
  }

  LogTrace("HappyPathSetup", NULL);

  Screen.print(3, " > Sensors");
  SensorInit();

  Screen.print(3, " > IoT Hub");
  DevKitMQTTClient_SetOption(OPTION_MINI_SOLUTION_NAME, "DevKit-GetStarted");
  DevKitMQTTClient_Init(true);

  DevKitMQTTClient_SetSendConfirmationCallback(SendConfirmationCallback);
  DevKitMQTTClient_SetMessageCallback(MessageCallback);
  DevKitMQTTClient_SetDeviceTwinCallback(DeviceTwinCallback);
  DevKitMQTTClient_SetDeviceMethodCallback(DeviceMethodCallback);
  DevKitMQTTClient_SetReportConfirmationCallback(DeviceReportConfirmationCallback);
  send_interval_ms = SystemTickCounterRead();

  // Init PB_0, PA5

  // extern green led
  pinMode(PB_0, OUTPUT);

  // relay output driver
  pinMode(PA_5, OUTPUT);

  // on-board user led
  pinMode(LED_USER, OUTPUT);

  // attach interrup service routine for USER_BUTTON_A
  attachInterrupt(USER_BUTTON_A, toggleUserLeds, CHANGE);
}

void DeviceReportConfirmationCallback(int status_code) 
{
  LogInfo("ReportConfirmationCallbak StatusCode %s", status_code);
}

void toggleUserLeds()
{
  buttonApressed = true;

  relayOn();
  userLEDOn();
  
  delay(3000);

  userLEDOff();
  relayOff();
}

void loop()
{
  if (hasWifi)
  {
    if (messageSending &&
        (int)(SystemTickCounterRead() - send_interval_ms) >= getInterval())
    {
      // Send  reported properties to IoT (Device-TWIN) 
      if (ReportProperties()) {
        LogInfo("Device-TWIN properties reported.");
      } else 
      {
        LogInfo("Device-TWIN properties not reported.");
      }

      // Send teperature data
      char messagePayload[MESSAGE_MAX_LEN];

      bool temperatureAlert = readMessage(messageCount++, messagePayload);
      LogInfo("Button A State: %s", buttonApressed ? "PRESSED" : "NOT PRESSED");

      EVENT_INSTANCE *message = DevKitMQTTClient_Event_Generate(messagePayload, MESSAGE);
      DevKitMQTTClient_Event_AddProp(message, "temperatureAlert", temperatureAlert ? "true" : "false");
      DevKitMQTTClient_Event_AddProp(message, "buttonApressed", buttonApressed ? "true" : "false");
      DevKitMQTTClient_SendEventInstance(message);


      send_interval_ms = SystemTickCounterRead();
      buttonApressed = false;
    }
    else
    {
      DevKitMQTTClient_Check();
    }
  }

  delay(1000);
}
