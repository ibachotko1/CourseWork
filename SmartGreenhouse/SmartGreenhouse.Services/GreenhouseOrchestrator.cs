using SmartGreenhouse.Core.Models;
using SmartGreenhouse.Services.ClimateControl;
using SmartGreenhouse.Services.Irrigation;
using SmartGreenhouse.Services.Lighting;
using SmartGreenhouse.Services.SensorDataServices;
using System;
using System.Collections.Generic;

namespace SmartGreenhouse.Services
{
    /// <summary>
    /// Главный координатор, объединяющий все сервисы
    /// </summary>
    public class GreenhouseOrchestrator
    {
        private readonly ClimateControlService _climateService;
        private readonly IrrigationService _irrigationService;
        private readonly LightingService _lightingService;
        private readonly SensorDataService _sensorDataService;
        private readonly ActuatorStatus _actuators;

        public GreenhouseOrchestrator()
        {
            _actuators = new ActuatorStatus();
            _climateService = new ClimateControlService(_actuators);
            _irrigationService = new IrrigationService(_actuators);
            _lightingService = new LightingService(_actuators);
            _sensorDataService = new SensorDataService();
        }

        public void RunCycle(List<SensorData> sensorReadings)
        {
            // ЛР3: Обработка данных с инвариантами
            _sensorDataService.ProcessSensorReadings(sensorReadings);

            var currentData = _sensorDataService.GetCurrentAverages();

            // ЛР1 + ЛР4: Управление системами на основе булевой логики и контрактов
            _climateService.AdjustClimate(currentData);
            _irrigationService.ManageIrrigation(currentData);
            _lightingService.ManageLighting(currentData);

            LogCurrentState(currentData);
        }

        private void LogCurrentState(SensorData data)
        {
            Console.WriteLine($"=== Greenhouse State ===");
            Console.WriteLine($"Temperature: {data.Temperature}°C, Humidity: {data.Humidity}%");
            Console.WriteLine($"Soil: {data.SoilMoisture}%, CO2: {data.CO2Level}ppm");
            Console.WriteLine($"Lights: {_actuators.Lights}, Water: {_actuators.WaterValve}");
            Console.WriteLine($"Heater: {_actuators.Heater}, Ventilation: {_actuators.Ventilation}");
            Console.WriteLine("========================");
        }

        public ActuatorStatus GetActuatorStatus() => _actuators;
        public List<SensorData> GetSensorHistory() => _sensorDataService.GetSensorHistory();
    }
}