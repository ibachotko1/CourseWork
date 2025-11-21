using System;

namespace SmartGreenhouse.Core.Models
{
    /// <summary>
    /// Данные с датчиков теплицы
    /// </summary>
    public class SensorData
    {
        public double Temperature { get; set; }        // °C
        public double Humidity { get; set; }           // %
        public double SoilMoisture { get; set; }       // %
        public double CO2Level { get; set; }           // ppm
        public double LightIntensity { get; set; }     // lux
        public bool IsRaining { get; set; }           // Дождь
        public DateTime Timestamp { get; set; }
    }
}