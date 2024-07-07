// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. 

#ifndef UTILITY_H
#define UTILITY_H

void parseTwinMessage(DEVICE_TWIN_UPDATE_STATE, const char *);
bool readMessage(int, char *);

void SensorInit(void);
void userLEDOn(void);
void userLEDOff(void);
void relayOn(void);
void relayOff(void);
void blinkLED(void);
void blinkSendConfirmation(void);
int getInterval(void);
bool ReportProperties(void);

extern int interval;
extern float temperature;
extern float temperatureThreshold;
extern bool triggerRelay;

#endif /* UTILITY_H */