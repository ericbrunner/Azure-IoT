// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. 

#include "DevKitMQTTClient.h"
#include "HTS221Sensor.h"
#include "LPS22HBSensor.h"
#include "AzureIotHub.h"
#include "Arduino.h"
#include "parson.h"
#include "config.h"
#include "RGB_LED.h"

#define RGB_LED_BRIGHTNESS 32

DevI2C *i2c;
HTS221Sensor *sensor;
LPS22HBSensor *pressureSensor;
static RGB_LED rgbLed;

static float humidity;
static float pressure;

int interval = INTERVAL;
float temperature  = 0;
float temperatureThreshold = 30;
bool triggerRelay = false;


int getInterval()
{
    return interval;
}

void relayOn() {
    digitalWrite(PA_5, HIGH);    
}

void relayOff() {
    digitalWrite(PA_5, LOW);
}

void userLEDOn() {
    digitalWrite(LED_USER, HIGH);
    digitalWrite(PB_0, HIGH);
}

void userLEDOff() {
    digitalWrite(LED_USER, LOW);
    digitalWrite(PB_0, LOW);
}

void blinkLED()
{
    rgbLed.turnOff();
    rgbLed.setColor(RGB_LED_BRIGHTNESS, 0, 0);
    delay(500);
    rgbLed.turnOff();
}

void blinkSendConfirmation()
{
    //rgbLed.turnOff();
    
    if (temperature > temperatureThreshold) {
        rgbLed.setColor(255, 0, RGB_LED_BRIGHTNESS);
    } else {
        rgbLed.setColor(0, 255, RGB_LED_BRIGHTNESS);
    }

    delay(500);

    if (temperature > temperatureThreshold) {
        rgbLed.setColor(255, 0, 0);
    } else {
        rgbLed.setColor(0, 255, 0);
    }

    //rgbLed.turnOff();

}

// void parseTwinMessage(DEVICE_TWIN_UPDATE_STATE updateState, const char *message)
// {
//     JSON_Value *root_value;
//     root_value = json_parse_string(message);
//     if (json_value_get_type(root_value) != JSONObject)
//     {
//         if (root_value != NULL)
//         {
//             json_value_free(root_value);
//         }
//         LogError("parse %s failed", message);
//         return;
//     }
//     JSON_Object *root_object = json_value_get_object(root_value);

//     double val = 0;
//     if (updateState == DEVICE_TWIN_UPDATE_COMPLETE)
//     {
//         JSON_Object *desired_object = json_object_get_object(root_object, "desired");
//         if (desired_object != NULL)
//         {
//             val = json_object_get_number(desired_object, "interval");
//         }
//     }
//     else
//     {
//         val = json_object_get_number(root_object, "interval");
//     }
//     if (val > 500)
//     {
//         interval = (int)val;
//         LogInfo(">>>Device twin updated: set interval to %d", interval);
//     }
//     json_value_free(root_value);
// }

void SensorInit()
{
    i2c = new DevI2C(D14, D15);
    sensor = new HTS221Sensor(*i2c);
    sensor->init(NULL);

    pressureSensor = new LPS22HBSensor(*i2c);
    pressureSensor -> init(NULL);

    humidity = -1;
    temperature = -1000;
    pressure = 0;
}

float readTemperature()
{
    sensor->reset();

    float temperature = 0;
    sensor->getTemperature(&temperature);

    return temperature;
}

float readHumidity()
{
    sensor->reset();

    float humidity = 0;
    sensor->getHumidity(&humidity);

    return humidity;
}

float readPressure() {

    float pressure = 0;
    pressureSensor->getPressure(&pressure);

    return pressure;
}

bool readMessage(int messageId, char *payload)
{
    JSON_Value *root_value = json_value_init_object();
    JSON_Object *root_object = json_value_get_object(root_value);
    char *serialized_string = NULL;

    json_object_set_number(root_object, "messageId", messageId);

    float t = readTemperature();
    float h = readHumidity();
    float p = readPressure();

    bool temperatureAlert = false;
    
    if(t != temperature)
    {
        temperature = t;
    }
    
    json_object_set_number(root_object, "temperature", temperature);
    

    if(temperature > temperatureThreshold)
    {
        temperatureAlert = true;
        rgbLed.setColor(255,0,0);
    } else {
        rgbLed.setColor(0,255,0);
    }
    
    if(h != humidity)
    {
        humidity = h;
    }
    
    if (p != pressure) 
    {
        pressure = p;
    } 

    json_object_set_number(root_object, "humidity", humidity);
    json_object_set_number(root_object, "pressure", pressure);

    serialized_string = json_serialize_to_string_pretty(root_value);

    snprintf(payload, MESSAGE_MAX_LEN, "%s", serialized_string);
    json_free_serialized_string(serialized_string);
    json_value_free(root_value);
    return temperatureAlert;
}

bool ReportProperties() 
{
    const char* template_string = "{\"interval\":\"%d\"}";

    char buffer[20];
    char *status_string = itoa(interval, buffer, 10);

    int size= strlen(status_string) + strlen(template_string) + 20;

    char* serialized_string = (char*)malloc(size);

    sprintf(serialized_string, "{\"interval\":\"%s\"}", status_string);
    serialized_string[size] = '\0';

    bool result = DevKitMQTTClient_ReportState(serialized_string);
    free(serialized_string);

    return result;
}
