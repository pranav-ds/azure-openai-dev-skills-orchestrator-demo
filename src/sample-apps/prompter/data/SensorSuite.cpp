/*MIT License

Copyright (c) 2024 Pranav Sumanth Doijode

For more information, please visit:
https://opensource.org/licenses/MIT
*/

#include "SensorSuite.hpp"
#include <iostream>

SensorSuite::SensorSuite() {
    
    // Create 3 proximity sensors
    proximitySensors.push_back(std::make_shared<ProximitySensor>("ps1", 1, true, 50e3, 10, true, "PS1", "ProximitySensor", "localhost", 5672));
    proximitySensors.push_back(std::make_shared<ProximitySensor>("ps2", 2, true, 50e3, 10, true, "PS2", "ProximitySensor", "localhost", 5672));
    proximitySensors.push_back(std::make_shared<ProximitySensor>("ps3", 2, true, 50e3, 10, true, "PS3", "ProximitySensor", "localhost", 5672));
    
    // Create a speed sensor
    speedSensor = std::make_shared<SpeedSensor>("ss1", 4, true, 50e3, 10, true, "SS1", "SpeedSensor", "localhost", 5672);
    
}

void SensorSuite::start() {
    // Start all sensors in their own threads and run them save thread id in a vector
    for (auto &sensor : proximitySensors)
    {
        sensorThreads.push_back(std::thread([&]() { sensor->start(); }));
    }
    sensorThreads.push_back(std::thread([&]() { speedSensor->start(); }));
}

void SensorSuite::stop() {
    
    // Stop all sensors
    for (auto &sensor : proximitySensors)
    {
        sensor->stop();
    }
    speedSensor->stop();

    // Join all sensor threads
    for (auto &thread : sensorThreads)
    {
        if (thread.joinable()) {
            thread.join();
        }
    }
    sensorThreads.clear();
}


SensorSuite::~SensorSuite()
{   
    stop();
}"