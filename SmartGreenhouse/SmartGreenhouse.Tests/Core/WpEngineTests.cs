using SmartGreenhouse.Core.WpCalculator;
using Xunit;
namespace SmartGreenhouse.Tests.Core
{
    public class WpEngineTests
    {
        [Fact]
        public void CalculateWateringPrecondition_ReturnsValidCondition()
        {
            // Act
            var result = WpEngine.CalculateWateringPrecondition();

            // Assert
            Assert.NotNull(result);
            // Проверяем, что результат содержит ожидаемые элементы после замены переменных
            Assert.Contains("5.0", result); // waterFlow заменен на 5.0
            Assert.Contains("currentTime", result);
            Assert.Contains("true", result); // valveOpen заменен на true
        }

        [Fact]
        public void WpSequence_WithComplexStatements_ProcessesCorrectly()
        {
            // Arrange
            var statements = new List<string>
            {
                "valveOpen := true",
                "waterFlow := 5.0",
                "timerStart := currentTime"
            };
            var postCondition = "valveOpen && waterFlow > 0 && timerStart <= currentTime";

            // Act
            var result = WpEngine.WpSequence(statements, postCondition);

            // Assert
            Assert.Equal("(true) && (5.0) > 0 && (currentTime) <= currentTime", result);
        }
    }
}
