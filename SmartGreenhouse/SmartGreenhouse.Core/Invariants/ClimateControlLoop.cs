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

            int j = 0;
            int totalCount = readings.Count;

            // ЛР3: Pre-условие
            Debug.Assert(totalCount > 0, "Sensor readings cannot be empty");

            // ЛР3: Инициализация инварианта
            Debug.Assert(InvariantHolds(0), "Invariant failed at initialization");

            while (j < totalCount)
            {
                // ЛР3: Проверка инварианта перед шагом
                Debug.Assert(InvariantHolds(j), $"Invariant failed before step {j}");

                // ЛР3: Проверка варианта-функции
                int currentVariant = VariantFunction(j, totalCount);
                Debug.Assert(currentVariant >= 0, $"Variant function negative at step {j}");

                // Обработка данных
                if (!IsInvalidReading(_sensorReadings[j]))
                {
                    ProcessReading(_sensorReadings[j]);
                    _processedIndices.Add(j);
                }

                j++;

                // ЛР3: Проверка инварианта после шага
                Debug.Assert(InvariantHolds(j), $"Invariant failed after step {j}");

                // ЛР3: Вариант-функция убывает
                int newVariant = VariantFunction(j, totalCount);
                Debug.Assert(newVariant < currentVariant, "Variant function did not decrease");
            }

            // ЛР3: Post-условие - все валидные данные обработаны
            Debug.Assert(_processedIndices.Count == CountValidReadings(readings),
                        "Not all valid readings were processed");
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







