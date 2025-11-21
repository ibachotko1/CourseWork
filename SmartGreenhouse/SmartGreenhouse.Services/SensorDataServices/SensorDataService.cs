using System;
using System.Collections.Generic;
using System.Linq;
using SmartGreenhouse.Core.Models;
using SmartGreenhouse.Core.Invariants;
using SmartGreenhouse.Core.BooleanLogic;

namespace SmartGreenhouse.Services.SensorDataServices
{
    /// <summary>
    /// ЛР3, ЛР4: Сервис работы с данными датчиков
    /// </summary>
    public class SensorDataService
    {
        private readonly List<SensorData> _sensorHistory;
        private readonly ClimateControlLoop _dataProcessingLoop;
        private readonly SensorDataValidator _validator;

        public SensorDataService()
        {
            _sensorHistory = new List<SensorData>();
            _dataProcessingLoop = new ClimateControlLoop();
            _validator = new SensorDataValidator();
        }

        public void ProcessSensorReadings(List<SensorData> readings)
        {
            // ЛР3: Обработка данных с инвариантами
            _dataProcessingLoop.ProcessSensorData(readings);

            // ЛР4: Валидация данных с помощью булевой логики
            var validReadings = readings.Where(r => _validator.IsValid(r)).ToList();

            // Сохранение в историю
            _sensorHistory.AddRange(validReadings);

            Console.WriteLine($"Processed {validReadings.Count} valid readings out of {readings.Count}");
        }

        public SensorData GetCurrentAverages()
        {
            if (_sensorHistory.Count == 0)
                return new SensorData();

            // Расчет средних значений за последний час
            var recentReadings = _sensorHistory
                .Where(r => r.Timestamp > DateTime.Now.AddHours(-1))
                .ToList();

            return new SensorData
            {
                Temperature = recentReadings.Average(r => r.Temperature),
                Humidity = recentReadings.Average(r => r.Humidity),
                SoilMoisture = recentReadings.Average(r => r.SoilMoisture),
                CO2Level = recentReadings.Average(r => r.CO2Level),
                LightIntensity = recentReadings.Average(r => r.LightIntensity),
                IsRaining = recentReadings.Any(r => r.IsRaining),
                Timestamp = DateTime.Now
            };
        }

        public List<SensorData> GetSensorHistory() => _sensorHistory;

        // ЛР4: Анализ корреляции между параметрами с помощью булевой логики
        public void AnalyzeCorrelations()
        {
            var truthTable = SensorLogic.GenerateTruthTable(3); // 3 параметра для анализа

            foreach (var row in truthTable)
            {
                // Анализ комбинаций условий
                bool highTemp = row[0];
                bool lowHumidity = row[1];
                bool highCO2 = row[2];

                // Логика выявления паттернов
                bool needsIntervention = highTemp && lowHumidity;
                Console.WriteLine($"Pattern: HighTemp={highTemp}, LowHumidity={lowHumidity}, HighCO2={highCO2} -> NeedsIntervention={needsIntervention}");
            }
        }
    }

    /// <summary>
    /// ЛР4: Валидатор данных датчиков на основе булевой логики
    /// </summary>
    public class SensorDataValidator
    {
        public bool IsValid(SensorData data)
        {
            // ЛР4: Булева логика валидации
            return IsTemperatureValid(data.Temperature) &&
                   IsHumidityValid(data.Humidity) &&
                   IsSoilMoistureValid(data.SoilMoisture) &&
                   IsCO2Valid(data.CO2Level) &&
                   IsLightIntensityValid(data.LightIntensity);
        }

        private bool IsTemperatureValid(double temp)
        {
            // Допустимый диапазон: -10°C до +50°C
            return temp >= -10.0 && temp <= 50.0;
        }

        private bool IsHumidityValid(double humidity)
        {
            // Допустимый диапазон: 0% до 100%
            return humidity >= 0.0 && humidity <= 100.0;
        }

        private bool IsSoilMoistureValid(double moisture)
        {
            // Допустимый диапазон: 0% до 100%
            return moisture >= 0.0 && moisture <= 100.0;
        }

        private bool IsCO2Valid(double co2)
        {
            // Допустимый диапазон: 300 ppm до 5000 ppm
            return co2 >= 300.0 && co2 <= 5000.0;
        }

        private bool IsLightIntensityValid(double light)
        {
            // Допустимый диапазон: 0 lux до 100000 lux
            return light >= 0.0 && light <= 100000.0;
        }
    }
}