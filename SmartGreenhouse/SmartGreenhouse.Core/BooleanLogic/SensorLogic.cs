using SmartGreenhouse.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartGreenhouse.Core.BooleanLogic
{
    /// <summary>
    /// ЛР4: Логика принятия решений на основе булевых функций
    /// 
    /// Этот класс содержит булевы функции, которые определяют, какие системы
    /// теплицы должны быть включены на основе показаний датчиков.
    /// Каждая функция возвращает true (включить) или false (выключить).
    /// 
    /// Это является примером применения булевой алгебры в реальной системе управления.
    /// </summary>
    public static class SensorLogic
    {
        /// <summary>
        /// ЛР4: Булева функция для определения необходимости включения полива
        /// 
        /// Формула: ShouldTurnOnWater = (soilMoisture < 60)
        /// Логика: Полив включается, если влажность почвы ниже нормального значения
        /// </summary>
        /// <param name="data">Данные с датчиков</param>
        /// <param name="actuators">Текущий статус исполнительных устройств</param>
        /// <returns>true - нужно включить полив, false - не нужно</returns>
        public static bool ShouldTurnOnWater(SensorData data, ActuatorStatus actuators)
        {
            // Простое условие: влажность почвы < 60%
            return data.SoilMoisture < 60.0;
        }

        /// <summary>
        /// ЛР4: Булева функция для определения необходимости включения обогрева
        /// 
        /// Формула: ShouldTurnOnHeating = (temperature < 15)
        /// Логика: Обогрев включается, если температура ниже минимального значения
        /// </summary>
        /// <param name="data">Данные с датчиков</param>
        /// <returns>true - нужно включить обогрев, false - не нужно</returns>
        public static bool ShouldTurnOnHeating(SensorData data)
        {
            // Простое условие: температура < 15°C
            return data.Temperature < 15.0;
        }

        /// <summary>
        /// ЛР4: Булева функция для определения необходимости включения вентиляции
        /// 
        /// Формула: ShouldTurnOnVentilation = (temperature > 25) ∨ (co2Level > 1200)
        /// Логика: Вентиляция включается, если температура слишком высокая 
        ///         ИЛИ уровень CO₂ слишком высокий
        /// Пример дизъюнкции (ЛОГИЧЕСКОГО ИЛИ) в булевой алгебре
        /// </summary>
        /// <param name="data">Данные с датчиков</param>
        /// <param name="actuators">Текущий статус исполнительных устройств</param>
        /// <returns>true - нужно включить вентиляцию, false - не нужно</returns>
        public static bool ShouldTurnOnVentilation(SensorData data, ActuatorStatus actuators)
        {
            // Дизъюнкция (ИЛИ): температура > 25°C ИЛИ CO₂ > 1200 ppm
            return data.Temperature > 25.0 || data.CO2Level > 1200;
        }

        /// <summary>
        /// ЛР4: Генерирует таблицу истинности для заданного количества переменных
        /// 
        /// Таблица истинности содержит все возможные комбинации значений булевых переменных.
        /// Для n переменных генерируется 2^n строк.
        /// </summary>
        /// <param name="variableCount">Количество булевых переменных</param>
        /// <returns>Список всех комбинаций значений</returns>
        public static List<bool[]> GenerateTruthTable(int variableCount)
        {
            var table = new List<bool[]>();
            int rowCount = 1 << variableCount; // 2^n - количество строк в таблице

            // Генерируем все комбинации от 0 до 2^n-1
            for (int i = 0; i < rowCount; i++)
            {
                var row = new bool[variableCount];
                // Преобразуем число i в двоичное представление
                for (int j = 0; j < variableCount; j++)
                {
                    // Проверяем j-ый бит: 0 = false, 1 = true
                    row[j] = (i & (1 << j)) != 0;
                }
                table.Add(row);
            }
            return table;
        }

        /// <summary>
        /// ЛР4: Генерирует ДНФ (Дизъюнктивную Нормальную Форму) из таблицы истинности
        /// 
        /// ДНФ - это дизъюнкция (ИЛИ) конъюнктов (И).
        /// Каждый конъюнкт соответствует строке таблицы, где результат = true.
        /// Пример: (x1 ∧ ¬x2) ∨ (x1 ∧ x2)
        /// </summary>
        /// <param name="results">Массив результатов булевой функции для каждой строки таблицы</param>
        /// <param name="variables">Массив имен переменных</param>
        /// <returns>Строка с ДНФ</returns>
        public static string GenerateDNF(bool[] results, string[] variables)
        {
            var terms = new List<string>();

            // Проходим по всем строкам таблицы истинности
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i])  // Если результат = true
                {
                    // Создаем конъюнкт (произведение) для этой строки
                    // ТОДО: реализация создания конъюнкта
                }
            }

            // Объединяем все конъюнкты через дизъюнкцию (∨ - ИЛИ)
            return string.Join(" ∨ ", terms);
        }

        private static bool IsMorning(DateTime timestamp) => timestamp.Hour >= 6 && timestamp.Hour < 10;
        private static bool IsDayTime(DateTime timestamp) => timestamp.Hour >= 6 && timestamp.Hour < 18;
    }
}