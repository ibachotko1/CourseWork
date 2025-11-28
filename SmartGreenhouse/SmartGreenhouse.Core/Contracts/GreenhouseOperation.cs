using SmartGreenhouse.Core.Models;
using System;
using System.Diagnostics;

namespace SmartGreenhouse.Core.Contracts
{
    /// <summary>
    /// ЛР1: Базовый абстрактный класс для операций теплицы с контрактами
    /// 
    /// КОНТРАКТ - это соглашение, которое описывает:
    /// - ПРЕДУСЛОВИЯ (Preconditions): что должно быть истинно ДО выполнения операции
    /// - ПОСТУСЛОВИЯ (Postconditions): что должно стать истинным ПОСЛЕ выполнения операции
    /// 
    /// Контракты помогают гарантировать корректность работы программы.
    /// </summary>
    public abstract class GreenhouseOperation
    {
        /// <summary>
        /// Название операции (например, "Start Irrigation", "Start Heating")
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// ЛР1: Флаг, показывающий, выполнены ли предусловия
        /// true - предусловия выполнены, операцию можно выполнять
        /// false - предусловия не выполнены, операцию нельзя выполнять
        /// </summary>
        public bool PreConditionsMet { get; protected set; }
        
        /// <summary>
        /// ЛР1: Флаг, показывающий, выполнены ли постусловия
        /// true - постусловия выполнены, операция завершилась успешно
        /// false - постусловия не выполнены, в операции ошибка
        /// </summary>
        public bool PostConditionsMet { get; protected set; }

        /// <summary>
        /// ЛР1: Проверяет предусловия (условия, которые должны быть выполнены ДО операции)
        /// </summary>
        /// <param name="data">Данные с датчиков</param>
        /// <param name="actuators">Статус исполнительных устройств</param>
        /// <returns>true - предусловия выполнены, false - не выполнены</returns>
        public abstract bool CheckPreConditions(SensorData data, ActuatorStatus actuators);
        
        /// <summary>
        /// Выполняет операцию (изменяет состояние исполнительных устройств)
        /// </summary>
        /// <param name="data">Данные с датчиков</param>
        /// <param name="actuators">Статус исполнительных устройств</param>
        public abstract void Execute(SensorData data, ActuatorStatus actuators);
        
        /// <summary>
        /// ЛР1: Проверяет постусловия (условия, которые должны быть выполнены ПОСЛЕ операции)
        /// </summary>
        /// <param name="data">Данные с датчиков</param>
        /// <param name="actuators">Статус исполнительных устройств</param>
        /// <returns>true - постусловия выполнены, false - не выполнены</returns>
        public abstract bool CheckPostConditions(SensorData data, ActuatorStatus actuators);

        /// <summary>
        /// ЛР1: Выполняет операцию с проверкой контрактов
        /// 
        /// Основной метод, который реализует полный цикл проверки контрактов:
        /// 1. Проверяет предусловия
        /// 2. Выполняет операцию
        /// 3. Проверяет постусловия
        /// </summary>
        /// <param name="data">Данные с датчиков</param>
        /// <param name="actuators">Статус исполнительных устройств</param>
        /// <exception cref="InvalidOperationException">Выбрасывается, если предусловия не выполнены</exception>
        public void PerformOperation(SensorData data, ActuatorStatus actuators)
        {
            // Шаг 1: Проверяем предусловия
            // Если предусловия не выполнены, операцию нельзя выполнять
            PreConditionsMet = CheckPreConditions(data, actuators);
            if (!PreConditionsMet)
                throw new InvalidOperationException($"Preconditions failed for {Name}");

            // Шаг 2: Выполняем саму операцию
            Execute(data, actuators);

            // Шаг 3: Проверяем постусловия
            // Если постусловия не выполнены, значит в операции ошибка
            PostConditionsMet = CheckPostConditions(data, actuators);
            Debug.Assert(PostConditionsMet, $"Postconditions failed for {Name}");
        }
    }

    /// <summary>
    /// ЛР1: Операция "Включение полива"
    /// 
    /// ПРЕДУСЛОВИЯ: влажность почвы < 60%
    /// ОПЕРАЦИЯ: открыть клапан полива
    /// ПОСТУСЛОВИЯ: клапан полива открыт (WaterValve = true)
    /// </summary>
    public class StartIrrigationOperation : GreenhouseOperation
    {
        public StartIrrigationOperation()
        {
            Name = "Start Irrigation";
        }

        /// <summary>
        /// ЛР1: Проверка предусловий
        /// ПРЕДУСЛОВИЕ: влажность почвы ниже 60% (почва сухая, нужен полив)
        /// </summary>
        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            // Проверяем: влажность почвы < 60%
            return data.SoilMoisture < 60.0;    // Почва сухая (ниже нормы 60-70%)
        }

        /// <summary>
        /// Выполнение операции: открыть клапан полива
        /// </summary>
        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            // Открываем клапан полива
            actuators.WaterValve = true;
        }

        /// <summary>
        /// ЛР1: Проверка постусловий
        /// ПОСТУСЛОВИЕ: клапан полива должен быть открыт
        /// </summary>
        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            // Проверяем: клапан открыт
            return actuators.WaterValve;  // Клапан должен быть открыт
        }
    }

    /// <summary>
    /// ЛР1: Операция "Включение обогрева"
    /// 
    /// ПРЕДУСЛОВИЯ: температура < 15°C
    /// ОПЕРАЦИЯ: включить обогреватель
    /// ПОСТУСЛОВИЯ: обогреватель включен (Heater = true)
    /// </summary>
    public class StartHeatingOperation : GreenhouseOperation
    {
        public StartHeatingOperation()
        {
            Name = "Start Heating";
        }

        /// <summary>
        /// ЛР1: Проверка предусловий
        /// ПРЕДУСЛОВИЕ: температура ниже 15°C (холодно, нужен обогрев)
        /// </summary>
        public override bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            // Проверяем: температура < 15°C
            return data.Temperature < 15.0;     // Холодно (ниже нормы 15-25°C)
        }

        /// <summary>
        /// Выполнение операции: включить обогреватель
        /// </summary>
        public override void Execute(SensorData data, ActuatorStatus actuators)
        {
            // Включаем обогреватель
            actuators.Heater = true;
        }

        /// <summary>
        /// ЛР1: Проверка постусловий
        /// ПОСТУСЛОВИЕ: обогреватель должен быть включен
        /// </summary>
        public override bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            // Проверяем: обогреватель включен
            return actuators.Heater;
        }
    }
}
