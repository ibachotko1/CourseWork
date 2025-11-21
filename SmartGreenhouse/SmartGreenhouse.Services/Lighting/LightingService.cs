using SmartGreenhouse.Core.BooleanLogic;
using SmartGreenhouse.Core.Contracts;
using SmartGreenhouse.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartGreenhouse.Services.Lighting
{
    /// <summary>
    /// ЛР1, ЛР4: Сервис управления освещением
    /// </summary>
    public class LightingService
    {
        private readonly ActuatorStatus _actuators;
        private LightingSchedule _schedule;

        // ЛР1: Операции освещения
        private readonly List<GreenhouseOperation> _lightingOperations;

        public LightingService(ActuatorStatus actuators)
        {
            _actuators = actuators;
            _schedule = new LightingSchedule();

            // ЛР1: Инициализация операций освещения
            _lightingOperations = new List<GreenhouseOperation>
            {
                new TurnOnLightsOperation(),
                new TurnOffLightsOperation(),
                new AdjustLightIntensityOperation()
            };
        }

        public void ManageLighting(SensorData data)
        {
            // ЛР4: Булева логика для управления освещением
            bool lightsShouldBeOn = ShouldTurnOnLights(data);

            if (lightsShouldBeOn && !_actuators.Lights)
            {
                var turnOnOp = _lightingOperations.OfType<TurnOnLightsOperation>().First();
                ExecuteWithContracts(turnOnOp, data);
            }
            else if (!lightsShouldBeOn && _actuators.Lights)
            {
                var turnOffOp = _lightingOperations.OfType<TurnOffLightsOperation>().First();
                ExecuteWithContracts(turnOffOp, data);
            }

            // Регулировка интенсивности если нужно
            if (_actuators.Lights)
            {
                var adjustOp = _lightingOperations.OfType<AdjustLightIntensityOperation>().First();
                ExecuteWithContracts(adjustOp, data);
            }
        }

        // ЛР4: Булевы функции для освещения
        public bool ShouldTurnOnLights(SensorData data)
        {
            // Используем время из данных, а не системное время
            bool isNight = IsNightTime(data.Timestamp);
            bool isDay = !isNight;
            bool isCloudy = IsCloudy(data);

            // Формула: (Низкая естественная освещенность И (ночь ИЛИ пасмурно)) ИЛИ
            //          (Рассада требует досветки) ИЛИ
            //          (По расписанию И не день)
            return (data.LightIntensity < 1000 && (isNight || isCloudy)) ||
                   (NeedsSeedlingLight() && isNight) ||
                   (_schedule.ShouldBeOn(data.Timestamp) && !isDay);
        }

        public void SetLightingSchedule(TimeSpan start, TimeSpan end, double intensity)
        {
            _schedule = new LightingSchedule { StartTime = start, EndTime = end, Intensity = intensity };
        }

        // ЛР4: Генерация таблицы истинности для логики освещения
        public void AnalyzeLightingLogic()
        {
            var truthTable = SensorLogic.GenerateTruthTable(4); // 4 переменные
            Console.WriteLine("Lighting logic truth table generated");

            // Анализ и минимизация логики
            foreach (var row in truthTable)
            {
                bool result = EvaluateLightingLogic(row[0], row[1], row[2], row[3]);
                Console.WriteLine($"Inputs: {string.Join(", ", row)} -> Result: {result}");
            }
        }

        private bool EvaluateLightingLogic(bool lowLight, bool isNight, bool needsSeedling, bool accordingToSchedule)
        {
            // Тот же алгоритм что в ShouldTurnOnLights, но для булевых входов
            return (lowLight && (isNight || true)) || // isCloudy упрощено до true для примера
                   (needsSeedling && isNight) ||
                   (accordingToSchedule && !isNight);
        }

        private void ExecuteWithContracts(GreenhouseOperation operation, SensorData data)
        {
            try
            {
                operation.PerformOperation(data, _actuators);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Lighting operation failed: {ex.Message}");
            }
        }

        private bool IsNightTime(DateTime timestamp) => timestamp.Hour < 6 || timestamp.Hour >= 18;
        private bool IsDayTime(DateTime timestamp) => !IsNightTime(timestamp);
        private bool IsCloudy(SensorData data) => data.LightIntensity < 5000;
        private bool NeedsSeedlingLight() => true; // Заглушка - в реальности проверка фазы роста
    }

    // ЛР1: Операции освещения

    public class TurnOnLightsOperation : GreenhouseOperation
    {
        public TurnOnLightsOperation() => Name = "Turn On Lights";

        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            return !actuators.Lights &&
                   data.LightIntensity < 10000; // Только если естественного света мало
        }

        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            actuators.Lights = true;
        }

        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            return actuators.Lights;
        }
    }

    public class TurnOffLightsOperation : GreenhouseOperation
    {
        public TurnOffLightsOperation() => Name = "Turn Off Lights";

        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            return actuators.Lights; // Свет должен быть включен
        }

        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            actuators.Lights = false;
        }

        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            return !actuators.Lights;
        }
    }

    public class AdjustLightIntensityOperation : GreenhouseOperation
    {
        public AdjustLightIntensityOperation() => Name = "Adjust Light Intensity";

        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            return actuators.Lights; // Свет должен быть включен
        }

        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            // Регулировка интенсивности на основе естественного освещения
            double targetIntensity = Math.Max(0, 10000 - data.LightIntensity);
            Console.WriteLine($"Adjusting light intensity to: {targetIntensity}");
        }

        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            return true; // Регулировка всегда успешна
        }
    }

    public class LightingSchedule
    {
        public TimeSpan StartTime { get; set; } = new TimeSpan(18, 0, 0); // 18:00
        public TimeSpan EndTime { get; set; } = new TimeSpan(6, 0, 0);   // 06:00
        public double Intensity { get; set; } = 0.8;

        public bool ShouldBeOn(DateTime currentTime)
        {
            var now = currentTime.TimeOfDay;
            return now >= StartTime || now <= EndTime;
        }
    }
}