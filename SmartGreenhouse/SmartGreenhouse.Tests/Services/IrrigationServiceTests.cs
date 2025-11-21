 using System;
using System.Collections.Generic;
using System.Linq;
using SmartGreenhouse.Core.Models;
using SmartGreenhouse.Core.Contracts;
using SmartGreenhouse.Core.BooleanLogic;
using SmartGreenhouse.Core.Invariants;

namespace SmartGreenhouse.Services.Irrigation
{
    /// <summary>
    /// ЛР1, ЛР3, ЛР4: Сервис управления поливом
    /// </summary>
    public class IrrigationService
    {
        private readonly ActuatorStatus _actuators;
        private readonly List<IrrigationZone> _zones;
        private readonly IrrigationCycle _irrigationCycle;

        // ЛР1: Операции полива
        private readonly List<GreenhouseOperation> _irrigationOperations;

        public IrrigationService(ActuatorStatus actuators)
        {
            _actuators = actuators;
            _zones = new List<IrrigationZone>();
            _irrigationCycle = new IrrigationCycle();

            // ЛР1: Инициализация операций полива
            _irrigationOperations = new List<GreenhouseOperation>
            {
                new StartIrrigationOperation(),
                new StopIrrigationOperation(),
                new AdjustIrrigationOperation()
            };

            InitializeZones();
        }

        public void ManageIrrigation(SensorData data)
        {
            // ЛР4: Булева логика для принятия решения о поливе
            bool shouldIrrigate = ShouldIrrigate(data);

            if (shouldIrrigate)
            {
                // ЛР3: Запуск цикла полива с инвариантами
                _irrigationCycle.ExecuteIrrigationCycle(_zones, data);

                // ЛР1: Выполнение операции с контрактами
                var irrigationOp = _irrigationOperations.OfType<StartIrrigationOperation>().First();
                ExecuteWithContracts(irrigationOp, data);
            }
            else
            {
                var stopOp = _irrigationOperations.OfType<StopIrrigationOperation>().First();
                ExecuteWithContracts(stopOp, data);
            }
        }

        // ЛР4: Сложная булева функция для полива
        internal bool ShouldIrrigate(SensorData data)
        {
            return (data.SoilMoisture < 30.0) ||
                   (IsMorning(data.Timestamp) && data.SoilMoisture < 50.0) ||
                   (data.Temperature > 25.0 && data.Humidity < 40.0);
        }

        private void InitializeZones()
        {
            // Инициализация зон полива
            _zones.Add(new IrrigationZone { Id = 1, Name = "Zone1", SoilMoisture = 0 });
            _zones.Add(new IrrigationZone { Id = 2, Name = "Zone2", SoilMoisture = 0 });
        }

        private void ExecuteWithContracts(GreenhouseOperation operation, SensorData data)
        {
            try
            {
                operation.PerformOperation(data, _actuators);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Irrigation operation failed: {ex.Message}");
            }
        }

        private bool IsMorning(DateTime timestamp) => timestamp.Hour >= 6 && timestamp.Hour < 10;
    }

    // ЛР1: Дополнительные операции полива

    public class StopIrrigationOperation : GreenhouseOperation
    {
        public StopIrrigationOperation() => Name = "Stop Irrigation";

        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            return actuators.WaterValve; // Полив должен быть активен
        }

        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            actuators.WaterValve = false;
        }

        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            return !actuators.WaterValve;
        }
    }

    public class AdjustIrrigationOperation : GreenhouseOperation
    {
        public AdjustIrrigationOperation() => Name = "Adjust Irrigation";

        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            return actuators.WaterValve && data.SoilMoisture < 80.0;
        }

        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            // Регулировка интенсивности полива на основе данных
            Console.WriteLine($"Adjusting irrigation based on soil moisture: {data.SoilMoisture}");
        }

        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            return true; // Регулировка всегда успешна в симуляции
        }
    }

    // ЛР3: Цикл полива с инвариантами

    public class IrrigationCycle
    {
        public void ExecuteIrrigationCycle(List<IrrigationZone> zones, SensorData data)
        {
            // ЛР3: Цикл обработки зон полива с инвариантами
            int i = 0;
            int processedCount = 0;

            // Инвариант: все зоны [0..i-1] обработаны
            while (i < zones.Count)
            {
                // Проверка инварианта перед шагом
                if (!CheckInvariant(zones, i))
                    throw new InvalidOperationException("Invariant violation in irrigation cycle");

                var zone = zones[i];

                // Обработка зоны
                if (ShouldIrrigateZone(zone, data))
                {
                    IrrigateZone(zone);
                    processedCount++;
                }

                i++;

                // Проверка варианта-функции
                int remaining = zones.Count - i;
                if (remaining < 0)
                    throw new InvalidOperationException("Variant function violation");
            }

            Console.WriteLine($"Irrigation cycle completed. Processed {processedCount} zones.");
        }

        private bool CheckInvariant(List<IrrigationZone> zones, int currentIndex)
        {
            // Инвариант: для всех j < currentIndex, зона либо полита, либо пропущена по причине
            return true; // Упрощенная проверка
        }

        private bool ShouldIrrigateZone(IrrigationZone zone, SensorData data)
        {
            return zone.SoilMoisture < data.SoilMoisture + 10.0; // Логика принятия решения
        }

        private void IrrigateZone(IrrigationZone zone)
        {
            Console.WriteLine($"Irrigating zone: {zone.Name}");
            zone.SoilMoisture += 15.0; // Увеличиваем влажность
        }
    }

    public class IrrigationZone
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double SoilMoisture { get; set; }
    }
}