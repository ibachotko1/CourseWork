using SmartGreenhouse.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartGreenhouse.Core.BooleanLogic
{
    /// <summary>
    /// ЛР4: Логика принятия решений на основе булевых функций
    /// </summary>
    public static class SensorLogic
    {
        // ЛР4: Булевы функции для принятия решений
        public static bool ShouldTurnOnWater(SensorData data, ActuatorStatus actuators)
        {
            return data.SoilMoisture < 60.0;
        }

        public static bool ShouldTurnOnHeating(SensorData data)
        {
            return data.Temperature < 15.0;
        }

        public static bool ShouldTurnOnVentilation(SensorData data, ActuatorStatus actuators)
        {
            return data.Temperature > 25.0 || data.CO2Level > 1200;
        }

        // ЛР4: Вспомогательные методы для таблиц истинности
        public static List<bool[]> GenerateTruthTable(int variableCount)
        {
            var table = new List<bool[]>();
            int rowCount = 1 << variableCount; // 2^n

            for (int i = 0; i < rowCount; i++)
            {
                var row = new bool[variableCount];
                for (int j = 0; j < variableCount; j++)
                {
                    row[j] = (i & (1 << j)) != 0;
                }
                table.Add(row);
            }
            return table;
        }

        // ЛР4: Генерация DNF из таблицы истинности
        public static string GenerateDNF(bool[] results, string[] variables)
        {
            var terms = new List<string>();

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i])
                {
                    // Создаем конъюнкт для строки где результат true
                }
            }

            return string.Join(" ∨ ", terms);
        }

        private static bool IsMorning(DateTime timestamp) => timestamp.Hour >= 6 && timestamp.Hour < 10;
        private static bool IsDayTime(DateTime timestamp) => timestamp.Hour >= 6 && timestamp.Hour < 18;
    }
}