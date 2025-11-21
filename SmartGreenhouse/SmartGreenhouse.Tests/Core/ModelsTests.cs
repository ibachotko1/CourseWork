using SmartGreenhouse.Core.Models;
using Xunit;

namespace SmartGreenhouse.Tests.Core
{
    public class ModelsTests
    {
        [Fact]
        public void SensorData_DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var sensorData = new SensorData();

            // Assert
            Assert.Equal(0, sensorData.Temperature);
            Assert.Equal(0, sensorData.Humidity);
            Assert.Equal(0, sensorData.SoilMoisture);
            Assert.Equal(0, sensorData.CO2Level);
            Assert.Equal(0, sensorData.LightIntensity);
            Assert.False(sensorData.IsRaining);
            Assert.Equal(DateTime.MinValue, sensorData.Timestamp);
        }

        [Fact]
        public void SensorData_PropertySetters_WorkCorrectly()
        {
            // Arrange
            var timestamp = DateTime.Now;
            var sensorData = new SensorData();

            // Act
            sensorData.Temperature = 25.5;
            sensorData.Humidity = 60.5;
            sensorData.SoilMoisture = 40.5;
            sensorData.CO2Level = 450.5;
            sensorData.LightIntensity = 10000.5;
            sensorData.IsRaining = true;
            sensorData.Timestamp = timestamp;

            // Assert
            Assert.Equal(25.5, sensorData.Temperature);
            Assert.Equal(60.5, sensorData.Humidity);
            Assert.Equal(40.5, sensorData.SoilMoisture);
            Assert.Equal(450.5, sensorData.CO2Level);
            Assert.Equal(10000.5, sensorData.LightIntensity);
            Assert.True(sensorData.IsRaining);
            Assert.Equal(timestamp, sensorData.Timestamp);
        }

        [Fact]
        public void ActuatorStatus_DefaultConstructor_SetsAllToFalse()
        {
            // Act
            var actuators = new ActuatorStatus();

            // Assert
            Assert.False(actuators.WaterValve);
            Assert.False(actuators.Heater);
            Assert.False(actuators.Ventilation);
            Assert.False(actuators.Lights);
            Assert.False(actuators.SunProtection);
        }

        [Fact]
        public void ActuatorStatus_PropertySetters_WorkCorrectly()
        {
            // Arrange
            var actuators = new ActuatorStatus();

            // Act
            actuators.WaterValve = true;
            actuators.Heater = true;
            actuators.Ventilation = true;
            actuators.Lights = true;
            actuators.SunProtection = true;

            // Assert
            Assert.True(actuators.WaterValve);
            Assert.True(actuators.Heater);
            Assert.True(actuators.Ventilation);
            Assert.True(actuators.Lights);
            Assert.True(actuators.SunProtection);
        }
    }
}
