using SmartGreenhouse.Core.BooleanLogic;
using SmartGreenhouse.Core.Models;
using Xunit;


namespace SmartGreenhouse.Tests.Core
{
    public class BooleanLogicTests
    {
        [Theory]
        [InlineData(10.0, true)]   // Низкая температура (< 15°C) - нужен обогрев
        [InlineData(5.0, true)]     // Очень низкая температура (< 15°C) - нужен обогрев
        [InlineData(14.9, true)]     // Температура чуть ниже порога (< 15°C) - нужен обогрев
        [InlineData(15.0, false)]    // Температура на пороге (>= 15°C) - обогрев не нужен
        [InlineData(20.0, false)]    // Нормальная температура (>= 15°C) - обогрев не нужен
        [InlineData(25.0, false)]   // Высокая температура (>= 15°C) - обогрев не нужен
        public void ShouldTurnOnHeating_CorrectConditions(double temp, bool expected)
        {
            // Arrange
            var data = new SensorData
            {
                Temperature = temp
            };

            // Act
            var result = SensorLogic.ShouldTurnOnHeating(data);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(25.0, true)]   // Низкая влажность почвы (< 60%)
        [InlineData(40.0, true)]   // Средняя влажность (< 60%)
        [InlineData(55.0, true)]   // Влажность чуть ниже порога (< 60%)
        [InlineData(60.0, false)]  // Влажность на пороге (>= 60%)
        [InlineData(70.0, false)]  // Высокая влажность (>= 60%)
        public void ShouldTurnOnWater_CorrectConditions(double soilMoisture, bool expected)
        {
            // Arrange
            var data = new SensorData
            {
                SoilMoisture = soilMoisture
            };
            var actuators = new ActuatorStatus();

            // Act
            var result = SensorLogic.ShouldTurnOnWater(data, actuators);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateTruthTable_CorrectSize()
        {
            // Act
            var table = SensorLogic.GenerateTruthTable(3);

            // Assert
            Assert.Equal(8, table.Count);
            Assert.All(table, row => Assert.Equal(3, row.Length));
        }
    }
}
