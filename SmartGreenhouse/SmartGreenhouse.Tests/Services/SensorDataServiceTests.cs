using SmartGreenhouse.Core.Models;
using SmartGreenhouse.Services.SensorDataServices;
using Xunit;

namespace SmartGreenhouse.Tests.Services
{
    public class SensorDataServiceTests
    {
        private readonly SensorDataService _service;

        public SensorDataServiceTests()
        {
            _service = new SensorDataService();
        }

        [Fact]
        public void ProcessSensorReadings_FiltersInvalidData()
        {
            // Arrange
            var readings = new List<SensorData>
            {
                new() {
                    Temperature = 25.0,
                    Humidity = 60.0,
                    SoilMoisture = 40.0,
                    CO2Level = 400,
                    LightIntensity = 5000,
                    Timestamp = DateTime.Now
                }, // Valid
                new() {
                    Temperature = -60.0,
                    Humidity = 60.0,
                    SoilMoisture = 40.0,
                    CO2Level = 400,
                    LightIntensity = 5000,
                    Timestamp = DateTime.Now
                }, // Invalid temperature
                new() {
                    Temperature = 25.0,
                    Humidity = 150.0,
                    SoilMoisture = 40.0,
                    CO2Level = 400,
                    LightIntensity = 5000,
                    Timestamp = DateTime.Now
                }  // Invalid humidity
            };

            // Act
            _service.ProcessSensorReadings(readings);

            // Assert
            var history = _service.GetSensorHistory();
            Assert.Single(history);
            Assert.Equal(25.0, history[0].Temperature);
        }

        [Fact]
        public void GetCurrentAverages_WithNoData_ReturnsEmptySensorData()
        {
            // Act
            var result = _service.GetCurrentAverages();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Temperature);
            Assert.Equal(0, result.Humidity);
        }

        [Fact]
        public void GetCurrentAverages_WithRecentData_CalculatesCorrectAverages()
        {
            // Arrange
            var readings = new List<SensorData>
            {
                new() {
                    Temperature = 20.0,
                    Humidity = 50.0,
                    SoilMoisture = 30.0,
                    CO2Level = 400,
                    LightIntensity = 10000,
                    Timestamp = DateTime.Now.AddMinutes(-30)
                },
                new() {
                    Temperature = 25.0,
                    Humidity = 60.0,
                    SoilMoisture = 40.0,
                    CO2Level = 500,
                    LightIntensity = 15000,
                    Timestamp = DateTime.Now.AddMinutes(-15)
                }
            };
            _service.ProcessSensorReadings(readings);

            // Act
            var result = _service.GetCurrentAverages();

            // Assert
            Assert.Equal(22.5, result.Temperature);
            Assert.Equal(55.0, result.Humidity);
            Assert.Equal(35.0, result.SoilMoisture);
        }

        [Fact]
        public void AnalyzeCorrelations_GeneratesTruthTable()
        {
            // Act & Assert - Should not throw
            var exception = Record.Exception(() => _service.AnalyzeCorrelations());
            Assert.Null(exception);
        }

        [Fact]
        public void ProcessSensorReadings_WithEmptyList_HandlesGracefully()
        {
            // Arrange
            var readings = new List<SensorData>();

            // Act
            var exception = Record.Exception(() => _service.ProcessSensorReadings(readings));

            // Assert
            Assert.Null(exception);
        }
    }

    /// <summary>
    /// ЛР4: Тесты валидатора данных датчиков на основе булевой логики
    /// </summary>
    public class SensorDataValidatorTests
    {
        private readonly SensorDataValidator _validator;

        public SensorDataValidatorTests()
        {
            _validator = new SensorDataValidator();
        }

        [Theory]
        [InlineData(-10, 50, 50, 400, 50000, true)]   // Valid boundary
        [InlineData(25, 50, 50, 400, 50000, true)]    // Valid normal
        [InlineData(-11, 50, 50, 400, 50000, false)]  // Invalid temperature low
        [InlineData(51, 50, 50, 400, 50000, false)]   // Invalid temperature high
        [InlineData(25, -1, 50, 400, 50000, false)]   // Invalid humidity low
        [InlineData(25, 101, 50, 400, 50000, false)]  // Invalid humidity high
        [InlineData(25, 50, -1, 400, 50000, false)]   // Invalid soil moisture low
        [InlineData(25, 50, 101, 400, 50000, false)]  // Invalid soil moisture high
        [InlineData(25, 50, 50, 299, 50000, false)]   // Invalid CO2 low
        [InlineData(25, 50, 50, 5001, 50000, false)]  // Invalid CO2 high
        [InlineData(25, 50, 50, 400, -1, false)]      // Invalid light low
        [InlineData(25, 50, 50, 400, 100001, false)]  // Invalid light high
        public void IsValid_ReturnsCorrectResult(double temp, double humidity, double soilMoisture, double co2, double light, bool expected)
        {
            // Arrange
            var data = new SensorData
            {
                Temperature = temp,
                Humidity = humidity,
                SoilMoisture = soilMoisture,
                CO2Level = co2,
                LightIntensity = light
            };

            // Act
            var result = _validator.IsValid(data);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
