using SmartGreenhouse.Core.Models;
using System;
using System.Diagnostics;

namespace SmartGreenhouse.Core.Contracts
{
    /// <summary>
    /// ЛР1: Базовый класс для операций с контрактами
    /// </summary>
    public abstract class GreenhouseOperation
    {
        public string Name { get; protected set; }

        // ЛР1: Индикаторы выполнения контрактов
        public bool PreConditionsMet { get; protected set; }
        public bool PostConditionsMet { get; protected set; }

        public abstract bool CheckPreConditions(SensorData data, ActuatorStatus actuators);
        public abstract void Execute(SensorData data, ActuatorStatus actuators);
        public abstract bool CheckPostConditions(SensorData data, ActuatorStatus actuators);

        public void PerformOperation(SensorData data, ActuatorStatus actuators)
        {
            // ЛР1: Проверка предусловий
            PreConditionsMet = CheckPreConditions(data, actuators);
            if (!PreConditionsMet)
                throw new InvalidOperationException($"Preconditions failed for {Name}");

            Execute(data, actuators);

            // ЛР1: Проверка постусловий
            PostConditionsMet = CheckPostConditions(data, actuators);
            Debug.Assert(PostConditionsMet, $"Postconditions failed for {Name}");
        }
    }

    /// <summary>
    /// ЛР1: Конкретная операция - Включение полива
    /// </summary>
    public class StartIrrigationOperation : GreenhouseOperation
    {
        public StartIrrigationOperation()
        {
            Name = "Start Irrigation";
        }

        // ЛР1: Реализация предусловий
        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            return data.SoilMoisture < 30.0 &&    // Почва сухая
                   !data.IsRaining &&             // Не идет дождь
                   data.Temperature > 5.0 &&      // Выше точки замерзания
                   !actuators.WaterValve;         // Полив еще не включен
        }

        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            actuators.WaterValve = true;
        }

        // ЛР1: Реализация постусловий
        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            return actuators.WaterValve;  // Клапан должен быть открыт
        }
    }

    /// <summary>
    /// ЛР1: Операция - Включение обогрева
    /// </summary>
    public class StartHeatingOperation : GreenhouseOperation
    {
        public StartHeatingOperation()
        {
            Name = "Start Heating";
        }

        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            return data.Temperature < 15.0 &&     // Холодно
                   !actuators.Heater;            // Обогрев выключен
        }

        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            actuators.Heater = true;
        }

        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            return actuators.Heater;
        }
    }
}