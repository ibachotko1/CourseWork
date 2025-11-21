using System;
using System.Collections.Generic;
using System.Linq;
using SmartGreenhouse.Core.Models;
using SmartGreenhouse.Core.Contracts;
using SmartGreenhouse.Core.BooleanLogic;
using SmartGreenhouse.Core.WpCalculator;

namespace SmartGreenhouse.Services.ClimateControl
{
    /// <summary>
    /// ЛР1, ЛР2, ЛР4: Сервис управления климатом теплицы
    /// </summary>
    public class ClimateControlService
    {
        private readonly ActuatorStatus _actuators;

        // ЛР1: Операции управления климатом
        private readonly List<GreenhouseOperation> _climateOperations;

        public ClimateControlService(ActuatorStatus actuators)
        {
            _actuators = actuators;

            // ЛР1: Инициализация операций климат-контроля
            _climateOperations = new List<GreenhouseOperation>
            {
                new StartHeatingOperation(),
                new StartVentilationOperation(),
                new StartCO2EnrichmentOperation()
            };
        }

        public void AdjustClimate(SensorData data)
        {
            // ЛР4: Булева логика принятия решений
            bool needHeating = ShouldHeat(data);
            bool needVentilation = ShouldVentilate(data);
            bool needCO2 = ShouldEnrichCO2(data);

            // ЛР2: Планирование последовательности действий с WP
            if (needHeating && needVentilation)
            {
                // Сложный сценарий - нужно и греть и проветривать
                string wpResult = PlanHeatingVentilationSequence(data);
                ExecuteClimateSequence(wpResult);
            }
            else
            {
                // ЛР1: Простые операции с проверкой контрактов
                if (needHeating)
                {
                    var heatingOp = _climateOperations.OfType<StartHeatingOperation>().First();
                    ExecuteWithContracts(heatingOp, data);
                }

                if (needVentilation)
                {
                    var ventilationOp = _climateOperations.OfType<StartVentilationOperation>().First();
                    ExecuteWithContracts(ventilationOp, data);
                }

                if (needCO2)
                {
                    var co2Op = _climateOperations.OfType<StartCO2EnrichmentOperation>().First();
                    ExecuteWithContracts(co2Op, data);
                }
            }
        }

        // ЛР4: Булевы функции для принятия решений
        public bool ShouldHeat(SensorData data)
        {
            // Формула: (Низкая температура И ночь) ИЛИ (Очень низкая температура)
            return (data.Temperature < 15.0 && IsNightTime()) || data.Temperature < 5.0;
        }

        public bool ShouldVentilate(SensorData data)
        {
            return (data.Temperature > 28.0 || data.CO2Level > 1500);
        }

        public bool ShouldEnrichCO2(SensorData data)
        {
            // Формула: Низкий CO2 И день И высокая освещенность
            return data.CO2Level < 800 && IsDayTime() && data.LightIntensity > 10000;
        }

        // ЛР2: Планирование сложной последовательности
        private string PlanHeatingVentilationSequence(SensorData data)
        {
            var statements = new List<string>
            {
                "tempThreshold := 20.0",
                "ventilationTime := currentTime + 300", // +5 минут
                "heaterPower := CalculateOptimalPower(data.Temperature)"
            };

            string postCondition = "temperature > tempThreshold && ventilationStartTime <= currentTime";

            return WpEngine.WpSequence(statements, postCondition);
        }

        private void ExecuteClimateSequence(string wpCondition)
        {
            // ЛР2: Исполнение последовательности на основе WP-расчета
            Console.WriteLine($"Executing climate sequence with WP: {wpCondition}");

            // Здесь будет реальная логика исполнения
            if (wpCondition.Contains("temperature") && wpCondition.Contains("ventilation"))
            {
                _actuators.Heater = true;
                _actuators.Ventilation = true;
            }
        }

        private void ExecuteWithContracts(GreenhouseOperation operation, SensorData data)
        {
            try
            {
                operation.PerformOperation(data, _actuators);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Operation failed: {ex.Message}");
            }
        }

        private bool IsDayTime() => DateTime.Now.Hour >= 6 && DateTime.Now.Hour < 18;
        private bool IsNightTime() => !IsDayTime();
    }

    // ЛР1: Конкретные операции для климат-контроля

    public class StartVentilationOperation : GreenhouseOperation
    {
        public StartVentilationOperation() => Name = "Start Ventilation";

        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            return data.CO2Level < 1000.0;
        }

        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            actuators.Ventilation = true;
        }

        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            return actuators.Ventilation;
        }
    }

    public class StartCO2EnrichmentOperation : GreenhouseOperation
    {
        public StartCO2EnrichmentOperation() => Name = "Start CO2 Enrichment";

        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            return data.CO2Level < 800 &&
                   IsDayTime(data.Timestamp) && // Используем время из данных
                   data.LightIntensity > 5000;
        }

        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            // В реальной системе здесь управление клапаном CO2
            Console.WriteLine("CO2 enrichment started");
        }

        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            return true;
        }

        private bool IsDayTime(DateTime timestamp) => timestamp.Hour >= 6 && timestamp.Hour < 18;
    }
}