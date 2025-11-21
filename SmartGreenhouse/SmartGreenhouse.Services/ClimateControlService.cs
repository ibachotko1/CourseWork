using SmartGreenhouse.Core.BooleanLogic;
using SmartGreenhouse.Core.Contracts;
using SmartGreenhouse.Core.Invariants;
using SmartGreenhouse.Core.Models;
using System.Collections.Generic;

namespace SmartGreenhouse.Services
{
    /// <summary>
    /// Основной сервис управления теплицей, интегрирующий все ЛР
    /// </summary>
    public class ClimateControlService
    {
        private readonly SensorData _currentData;
        private readonly ActuatorStatus _actuators;
        private readonly ClimateControlLoop _controlLoop;

        // ЛР1: Операции с контрактами
        private readonly List<GreenhouseOperation> _operations;

        public ClimateControlService()
        {
            _currentData = new SensorData();
            _actuators = new ActuatorStatus();
            _controlLoop = new ClimateControlLoop();

            // ЛР1: Инициализация операций
            _operations = new List<GreenhouseOperation>
            {
                new StartIrrigationOperation(),
                new StartHeatingOperation()
            };
        }

        public void ProcessClimateControl(List<SensorData> sensorReadings)
        {
            // ЛР3: Обработка данных с инвариантами
            _controlLoop.ProcessSensorData(sensorReadings);

            // ЛР4: Принятие решений на основе булевой логики
            bool shouldWater = SensorLogic.ShouldTurnOnWater(_currentData, _actuators);
            bool shouldHeat = SensorLogic.ShouldTurnOnHeating(_currentData);
            bool shouldVentilate = SensorLogic.ShouldTurnOnVentilation(_currentData, _actuators);

            // ЛР1: Выполнение операций с проверкой контрактов
            if (shouldWater)
            {
                var irrigationOp = _operations.OfType<StartIrrigationOperation>().First();
                irrigationOp.PerformOperation(_currentData, _actuators);
            }

            // ЛР2: Использование WP для планирования сложных последовательностей
            if (shouldHeat && shouldVentilate)
            {
                PlanHeatingVentilationSequence();
            }
        }

        private void PlanHeatingVentilationSequence()
        {
            // ЛР2: Планирование последовательности с WP-калькулятором
            string wpCondition = WpEngine.CalculateWateringPrecondition();
            // Используем вычисленное предусловие для принятия решения
        }

        // ЛР4: Методы для работы с таблицами истинности
        public List<bool[]> GetTruthTableForWateringLogic()
        {
            return SensorLogic.GenerateTruthTable(3); // 3 переменные: влажность, дождь, время
        }
    }
}