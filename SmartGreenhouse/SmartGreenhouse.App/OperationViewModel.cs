using SmartGreenhouse.Core.Contracts;
using SmartGreenhouse.Core.Models;
using System;
using System.ComponentModel;

namespace SmartGreenhouse.App
{
    public class OperationViewModel : INotifyPropertyChanged
    {
        private readonly GreenhouseOperation _operation;
        private readonly Func<SensorData, ActuatorStatus, bool> _preConditionChecker;
        private readonly Func<SensorData, ActuatorStatus, bool> _postConditionChecker;
        private readonly Action<SensorData, ActuatorStatus> _executeAction;

        public OperationViewModel(
            string name,
            GreenhouseOperation operation,
            Func<SensorData, ActuatorStatus, bool> preConditionChecker,
            Func<SensorData, ActuatorStatus, bool> postConditionChecker,
            Action<SensorData, ActuatorStatus> executeAction)
        {
            Name = name;
            _operation = operation;
            _preConditionChecker = preConditionChecker;
            _postConditionChecker = postConditionChecker;
            _executeAction = executeAction;
        }

        public string Name { get; }

        public GreenhouseOperation Operation => _operation;

        public bool CheckPreConditions(SensorData data, ActuatorStatus actuators)
        {
            return _preConditionChecker(data, actuators);
        }

        public bool CheckPostConditions(SensorData data, ActuatorStatus actuators)
        {
            return _postConditionChecker(data, actuators);
        }

        public void Execute(SensorData data, ActuatorStatus actuators)
        {
            _executeAction(data, actuators);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

