#include <Arduino.h>
#include <Wire.h>

#include "SFM3300.h"

SFM3300 sfm3300(64);

#define RESET_SFM_PIN 13
#define SFM3300_offSet 32768.00
#define SFM3300_scaleFactor 120.00
#define delayPeriod 50

double airFlow = 0;
double tidalVolume = 0;
unsigned long previousMillis = 0;

void setup()
{
    Wire.begin();
    sfm3300.begin();
    pinMode(RESET_SFM_PIN, OUTPUT);

    digitalWrite(13, HIGH);

    Serial.begin(115200);
    Serial.println("FTV Online!");
}

void RebootSensor()
{

    digitalWrite(RESET_SFM_PIN, LOW);
    delay(500);
    digitalWrite(RESET_SFM_PIN, HIGH);
    delay(100);

    sfm3300.begin();

    delay(100);

    airFlow = 0;
    tidalVolume = 0;
}

void loop()
{
    int rawData = 0;
    if (!sfm3300.getValue(&rawData))
    {
        Serial.println("ERROR: GetValue Reseting unit");
        RebootSensor();
        return;
    }

    airFlow = ((double(rawData) - SFM3300_offSet) / SFM3300_scaleFactor);
    if (floor(airFlow) == -273 || airFlow == 57.65)
    {
        Serial.println("ERROR: Reading Reseting unit");
        RebootSensor();
        return;
    }

    // Check the flow triggers (inspiration and expiration):
    if (abs(airFlow) > 0.50)
    {
        // Calculate the tidal volume in mL @ every 50ms:
        tidalVolume = tidalVolume + ((airFlow / 60) * (millis() - previousMillis));
        previousMillis = millis();
        if (tidalVolume < 0.00)
        {
            tidalVolume = 0.00;
        }
    }
    else if((millis() - previousMillis) > 10000)
    {
        tidalVolume = 0.00;
    }

    // Normalized the waveform for plotting
    double normFlow = airFlow / 30;
    double normVol = tidalVolume / 200;

    // Plot the normilized waveform; use serial monitor
    Serial.print("NF ");
    Serial.println(normFlow);

    Serial.print("NV ");
    Serial.println(normVol);

    Serial.print("RV ");
    Serial.println(rawData);

    delay(delayPeriod);
}

/*********
  Rui Santos
  Complete project details at https://randomnerdtutorials.com
*********/
// #include <Arduino.h>
// #include <Wire.h>

// void setup()
// {
//     Wire.begin();
//     Serial.begin(115200);
//     Serial.println("\nI2C Scanner");
// }

// void loop()
// {
//     byte error, address;
//     int nDevices;
//     Serial.println("Scanning...");
//     nDevices = 0;
//     for (address = 1; address < 127; address++)
//     {
//         Wire.beginTransmission(address);
//         error = Wire.endTransmission();
//         if (error == 0)
//         {
//             Serial.print("\nI2C device found at address 0x");
//             if (address < 16)
//             {
//                 Serial.print("0");
//             }
//             Serial.println(address, HEX);
//             nDevices++;
//         }
//         else
//         {
//            Serial.print(".");
//         }
//     }
//     if (nDevices == 0)
//     {
//         Serial.println("No I2C devices found\n");
//     }
//     else
//     {
//         Serial.println("done\n");
//     }
//     delay(5000);
// }
