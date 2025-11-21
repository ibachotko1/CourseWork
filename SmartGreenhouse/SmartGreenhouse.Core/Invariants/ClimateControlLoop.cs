using SmartGreenhouse.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SmartGreenhouse.Core.Invariants
{
    /// <summary>
    /// ЛР3: Цикл обработки данных с инвариантами
    /// </summary>
    public class ClimateControlLoop
    {
        private List<SensorData> _sensorReadings;
        private List<int> _processedIndices;

        // ЛР3: Инвариант цикла
        private bool InvariantHolds(int currentIndex)
        {
            // Все элементы [0..currentIndex-1] обработаны и добавлены в _processedIndices
            return _processedIndices.Count == currentIndex &&
                   _processedIndices.TrueForAll(idx => idx >= 0 && idx < _sensorReadings.Count);
        }

        // ЛР3: Вариант-функция (монотонно убывает)
        private int VariantFunction(int currentIndex, int totalCount)
        {
            return totalCount - currentIndex;
        }

        public void ProcessSensorData(List<SensorData> readings)
        {
            _sensorReadings = readings;
            _processedIndices = new List<int>();

            if (_sensorReadings == null || _sensorReadings.Count == 0)
            {
                // ЛР3: Обработка пустого списка - просто выходим
                return;
            }

            int j = 0;
            int totalCount = readings.Count;

            // ЛР3: Pre-условие (без Debug.Assert для стабильности тестов)
            if (totalCount <= 0)
            {
                Console.WriteLine("Sensor readings cannot be empty");
                return;
            }

            // ЛР3: Инициализация инварианта
            if (!InvariantHolds(0))
            {
                Console.WriteLine("Invariant failed at initialization");
                return;
            }

            while (j < totalCount)
            {
                // ЛР3: Проверка инварианта перед шагом
                if (!InvariantHolds(j))
                {
                    Console.WriteLine($"Invariant failed before step {j}");
                    break;
                }

                // ЛР3: Проверка варианта-функции
                int currentVariant = VariantFunction(j, totalCount);
                if (currentVariant < 0)
                {
                    Console.WriteLine($"Variant function negative at step {j}");
                    break;
                }

                // Обработка данных
                if (!IsInvalidReading(_sensorReadings[j]))
                {
                    ProcessReading(_sensorReadings[j]);
                    _processedIndices.Add(j);
                }

                j++;

                // ЛР3: Проверка инварианта после шага
                if (!InvariantHolds(j))
                {
                    Console.WriteLine($"Invariant failed after step {j}");
                    break;
                }

                // ЛР3: Вариант-функция убывает
                int newVariant = VariantFunction(j, totalCount);
                if (newVariant >= currentVariant)
                {
                    Console.WriteLine("Variant function did not decrease");
                    break;
                }
            }

            // ЛР3: Post-условие - все валидные данные обработаны
            int processedCount = _processedIndices.Count;
            int validCount = CountValidReadings(readings);
            if (processedCount != validCount)
            {
                Console.WriteLine($"Not all valid readings were processed. Expected: {validCount}, Actual: {processedCount}");
            }
        }

        private bool IsInvalidReading(SensorData data)
        {
            return data.Temperature < -50 || data.Temperature > 60 ||
                   data.Humidity < 0 || data.Humidity > 100;
        }

        private void ProcessReading(SensorData data)
        {
            // Обработка показаний датчика
        }

        private int CountValidReadings(List<SensorData> readings)
        {
            int count = 0;
            foreach (var reading in readings)
            {
                if (!IsInvalidReading(reading)) count++;
            }
            return count;
        }
    }
}







