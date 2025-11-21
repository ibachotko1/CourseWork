using SmartGreenhouse.Core.Contracts;
using SmartGreenhouse.Core.Models;
using SmartGreenhouse.Services.ClimateControl;
using Xunit;

namespace SmartGreenhouse.Tests.Core
{
    public class GreenhouseOperationTests
    {
        [Fact]
        public void StartCO2EnrichmentOperation_WhenConditionsMet_ExecutesSuccessfully()
        {
            // Arrange
            var operation = new StartCO2EnrichmentOperation();
            var data = new SensorData
            {
                CO2Level = 600,
                LightIntensity = 6000,
                Timestamp = DateTime.Parse("2024-01-01 12:00") // День
            };
            var actuators = new ActuatorStatus();

            // Act & Assert
            var exception = Record.Exception(() => operation.PerformOperation(data, actuators));
            Assert.Null(exception);
        }

        [Fact]
        public void StartCO2EnrichmentOperation_WhenCO2High_ThrowsException()
        {
            // Arrange
            var operation = new StartCO2EnrichmentOperation();
            var data = new SensorData
            {
                CO2Level = 900, // Слишком высокий
                LightIntensity = 6000,
                Timestamp = DateTime.Parse("2024-01-01 12:00")
            };
            var actuators = new ActuatorStatus();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                operation.PerformOperation(data, actuators));
        }

        [Fact]
        public void StartCO2EnrichmentOperation_WhenNotDayTime_ThrowsException()
        {
            // Arrange
            var operation = new StartCO2EnrichmentOperation();
            var data = new SensorData
            {
                CO2Level = 600,
                LightIntensity = 6000,
                Timestamp = DateTime.Parse("2024-01-01 20:00") // Ночь
            };
            var actuators = new ActuatorStatus();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                operation.PerformOperation(data, actuators));
        }
    }
}
