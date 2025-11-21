using SmartGreenhouse.Core.Models;
using SmartGreenhouse.Services.Lighting;
using Xunit;

namespace SmartGreenhouse.Tests.Services
{
    public class LightingServiceTests
    {
        private readonly LightingService _service;
        private readonly ActuatorStatus _actuators;

        public LightingServiceTests()
        {
            _actuators = new ActuatorStatus();
            _service = new LightingService(_actuators);
        }

        [Theory]
        [InlineData(800, true)]   // Ночью + низкая освещенность
        [InlineData(20000, false)] // День + высокая освещенность
        public void ShouldTurnOnLights_UnderDifferentConditions(double lightIntensity, bool expected)
        {
            // Arrange
            var data = new SensorData
            {
                LightIntensity = lightIntensity,
                Timestamp = expected ?
                    DateTime.Parse("2024-01-01 20:00") : // Ночь
                    DateTime.Parse("2024-01-01 12:00")   // День
            };

            // Act
            var result = _service.ShouldTurnOnLights(data);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ManageLighting_WhenHighLightAtDay_TurnsOffLights()
        {
            // Arrange
            _actuators.Lights = true; // Предварительно включаем свет
            var data = new SensorData
            {
                LightIntensity = 20000,
                Timestamp = DateTime.Parse("2024-01-01 12:00") // День
            };

            // Act
            _service.ManageLighting(data);

            // Assert
            Assert.False(_actuators.Lights);
        }

        [Fact]
        public void ManageLighting_WhenLowLightAtNight_TurnsOnLights()
        {
            // Arrange
            var data = new SensorData
            {
                LightIntensity = 800,
                Timestamp = DateTime.Parse("2024-01-01 20:00") // Ночь
            };

            // Act
            _service.ManageLighting(data);

            // Assert
            Assert.True(_actuators.Lights);
        }
    }
}