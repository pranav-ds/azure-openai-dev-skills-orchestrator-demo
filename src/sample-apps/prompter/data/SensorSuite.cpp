/*MIT License

Copyright (c) 2024 Pranav Sumanth Doijode

For more information, please visit:
https://opensource.org/licenses/MIT
*/

/*
MIT License

Copyright (c) 2024 Pranav Sumanth Doijode

For more information, please visit:
https://opensource.org/licenses/MIT
*/

#include <iostream>
#include <vector>
#include <thread>
#include "ProximitySensor.hpp"
#include "SpeedSensor.hpp"

class Sensor {
public:
    Sensor( std::string sensorName,
            int sensorID,
            bool sensorUp, 
            double sensorFrequency, 
            double conversionFactor, 
            bool simulateSensor,
            const std::string& queue_name, 
            const std::string& exchange_name,
            const std::string& host,
            int port);
    ~Sensor();

    void start();
    void stop();
    void publishData(const std::string& data);
    int getSensorID();

private:
    std::string sensorName;
    int sensorID;
    AmqpClient::Channel::OpenOpts opts;
    AmqpClient::Channel::ptr_t channel;
    std::default_random_engine generator;
    std::normal_distribution<double> distribution;
    std::string queue_name;
    std::string exchange_name;
    bool sensorUp;
    double sensorFrequency; // Units in Hz
    bool simulateSensor;
    double conversionFactor;
    std::mutex mtx;
};

class ProximitySensor: public Sensor
{
public:
    using Sensor::Sensor;
    ~ProximitySensor();
};

// This class initalizes 3 proximity sensors and 1 speed sensor
class SensorSuite
{
public:
    SensorSuite();
    ~SensorSuite();
    void instantiateSensors();
    void start();
    void stop();
    std::vector<std::shared_ptr<ProximitySensor>> proximitySensors;
    std::shared_ptr<SpeedSensor> speedSensor;

private:
    std::vector<std::thread> sensorThreads;
};


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