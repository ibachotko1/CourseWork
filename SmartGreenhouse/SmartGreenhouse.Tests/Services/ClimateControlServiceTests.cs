using SmartGreenhouse.Core.Models;
using SmartGreenhouse.Services.ClimateControl;
using Xunit;

namespace SmartGreenhouse.Tests.Services
{
    public class ClimateControlServiceTests
    {
        private readonly ClimateControlService _service;
        private readonly ActuatorStatus _actuators;

        public ClimateControlServiceTests()
        {
            _actuators = new ActuatorStatus();
            _service = new ClimateControlService(_actuators);
        }

        [Theory]
        [InlineData(4.0, true)]    // Очень низкая температура
        [InlineData(14.0, true)]   // Низкая температура ночью
        [InlineData(20.0, false)]  // Нормальная температура
        public void ShouldHeat_ReturnsCorrectValue(double temperature, bool expected)
        {
            // Arrange
            var data = new SensorData
            {
                Temperature = temperature,
                Timestamp = DateTime.Now.Hour < 6 || DateTime.Now.Hour >= 18 ?
                    DateTime.Parse("2024-01-01 20:00") : // Ночь
                    DateTime.Parse("2024-01-01 12:00")   // День
            };

            // Act
            var result = _service.ShouldHeat(data);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(29.0, 1600, true)]   // Высокая температура + вентиляция
        [InlineData(20.0, 1600, true)]   // Высокий CO2
        [InlineData(20.0, 1200, false)]  // Нормальные условия
        public void ShouldVentilate_ReturnsCorrectValue(double temperature, double co2, bool expected)
        {
            // Arrange
            var data = new SensorData
            {
                Temperature = temperature,
                CO2Level = co2
            };

            // Act
            var result = _service.ShouldVentilate(data);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void AdjustClimate_WhenNeedHeating_TurnsOnHeater()
        {
            // Arrange
            var data = new SensorData { Temperature = 4.0 };

            // Act
            _service.AdjustClimate(data);

            // Assert
            Assert.True(_actuators.Heater);
        }

        [Fact]
        public void AdjustClimate_WhenNeedVentilation_TurnsOnVentilation()
        {
            // Arrange
            var data = new SensorData
            {
                Temperature = 29.0
            };

            // Act
            _service.AdjustClimate(data);

            // Assert
            Assert.True(_actuators.Ventilation);
        }
    }
}