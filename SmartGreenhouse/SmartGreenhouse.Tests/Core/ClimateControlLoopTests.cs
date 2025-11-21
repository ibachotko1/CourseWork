using SmartGreenhouse.Core.Invariants;
using SmartGreenhouse.Core.Models;
using System.Diagnostics;
using Xunit;

namespace SmartGreenhouse.Tests.Core
{
    public class ClimateControlLoop
    {
        private List<SensorData> _sensorReadings;
        private List<int> _processedIndices;

        // Исправленный инвариант
        private bool InvariantHolds(int currentIndex)
        {
            // Все элементы [0..currentIndex-1] обработаны и валидные добавлены в _processedIndices
            // Невалидные данные пропускаются, поэтому _processedIndices.Count <= currentIndex
            return _processedIndices.Count <= currentIndex &&
                   _processedIndices.TrueForAll(idx => idx >= 0 && idx < _sensorReadings.Count) &&
                   _processedIndices.Count == CountValidReadingsUpTo(currentIndex);
        }

        private int CountValidReadingsUpTo(int index)
        {
            int count = 0;
            for (int i = 0; i < index && i < _sensorReadings.Count; i++)
            {
                if (!IsInvalidReading(_sensorReadings[i])) count++;
            }
            return count;
        }

        private int VariantFunction(int currentIndex, int totalCount)
        {
            return totalCount - currentIndex;
        }

        public void ProcessSensorData(List<SensorData> readings)
        {
            _sensorReadings = readings ?? new List<SensorData>();
            _processedIndices = new List<int>();

            if (_sensorReadings.Count == 0)
            {
                Console.WriteLine("No sensor readings to process");
                return;
            }

            int j = 0;
            int totalCount = _sensorReadings.Count;

            // Pre-условие
            Debug.Assert(totalCount > 0, "Sensor readings cannot be empty");

            // Инициализация инварианта
            Debug.Assert(InvariantHolds(0), "Invariant failed at initialization");

            while (j < totalCount)
            {
                // Проверка инварианта перед шагом
                if (!InvariantHolds(j))
                {
                    Console.WriteLine($"Invariant failed before step {j}");
                    break;
                }

                // Проверка варианта-функции
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

                // Проверка инварианта после шага
                if (!InvariantHolds(j))
                {
                    Console.WriteLine($"Invariant failed after step {j}");
                    break;
                }

                // Вариант-функция должна убывать
                int newVariant = VariantFunction(j, totalCount);
                if (newVariant >= currentVariant)
                {
                    Console.WriteLine("Variant function did not decrease");
                    break;
                }
            }

            // Post-условие - все валидные данные обработаны
            int totalValid = CountValidReadings(_sensorReadings);
            if (_processedIndices.Count != totalValid)
            {
                Console.WriteLine($"Not all valid readings were processed. Expected: {totalValid}, Actual: {_processedIndices.Count}");
            }
        }

        private bool IsInvalidReading(SensorData data)
        {
            return data == null ||
                   data.Temperature < -50 || data.Temperature > 60 ||
                   data.Humidity < 0 || data.Humidity > 100;
        }

        private void ProcessReading(SensorData data)
        {
            // Реальная обработка показаний датчика
            Console.WriteLine($"Processing reading: Temp={data.Temperature}, Humidity={data.Humidity}");
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
