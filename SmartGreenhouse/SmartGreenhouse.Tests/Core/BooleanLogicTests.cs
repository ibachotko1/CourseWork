using SmartGreenhouse.Core.BooleanLogic;
using SmartGreenhouse.Core.Models;
using Xunit;


namespace SmartGreenhouse.Tests.Core
{
    public class BooleanLogicTests
    {
        [Theory]
        [InlineData(10.0, false, 1800, true)]   // Низкая температура + ночь + низкий CO2
        [InlineData(20.0, false, 1800, false)]  // Нормальная температура
        [InlineData(10.0, true, 1800, false)]   // Низкая температура + день
        [InlineData(10.0, false, 2200, false)]  // Низкая температура + ночь + высокий CO2
        public void ShouldTurnOnHeating_CorrectConditions(double temp, bool isDayTime, double co2, bool expected)
        {
            // Arrange
            var data = new SensorData
            {
                Temperature = temp,
                CO2Level = co2,
                Timestamp = isDayTime ?
                    DateTime.Parse("2024-01-01 12:00") :
                    DateTime.Parse("2024-01-01 20:00")
            };

            // Act
            var result = SensorLogic.ShouldTurnOnHeating(data);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(25.0, false, true)]   // Низкая влажность почвы + нет дождя
        [InlineData(40.0, false, false)]  // Средняя влажность
        [InlineData(25.0, true, false)]   // Низкая влажность + дождь
        [InlineData(60.0, false, false)]  // Высокая влажность
        public void ShouldTurnOnWater_CorrectConditions(double soilMoisture, bool isRaining, bool expected)
        {
            // Arrange
            var data = new SensorData
            {
                SoilMoisture = soilMoisture,
                IsRaining = isRaining
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
