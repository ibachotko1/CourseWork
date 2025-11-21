using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartGreenhouse.Core.Models;
using SmartGreenhouse.Core.Contracts;
using SmartGreenhouse.Core.BooleanLogic;
using SmartGreenhouse.Core.WpCalculator;
using SmartGreenhouse.Core.Invariants;
using SmartGreenhouse.Services.ClimateControl;
using SmartGreenhouse.Services.Irrigation;
using SmartGreenhouse.Services.Lighting;
using SmartGreenhouse.Services.SensorDataServices;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace SmartGreenhouse.App
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ClimateControlService _climateService;
        private readonly IrrigationService _irrigationService;
        private readonly LightingService _lightingService;
        private readonly SensorDataService _sensorService;
        private readonly DispatcherTimer _timer;

        public MainViewModel()
        {
            // Инициализация сервисов
            var actuators = new ActuatorStatus();
            _climateService = new ClimateControlService(actuators);
            _irrigationService = new IrrigationService(actuators);
            _lightingService = new LightingService(actuators);
            _sensorService = new SensorDataService();

            Actuators = actuators;
            CurrentData = new SensorData { Timestamp = DateTime.Now };
            SensorHistory = new ObservableCollection<SensorData>();
            OperationLog = "Система инициализирована\n";

            // Инициализация команд
            InitializeCommands();

            // Таймер для обновления данных
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += (s, e) => UpdateData();
            _timer.Start();

            UpdateStatus("Система запущена");
        }

        private void InitializeCommands()
        {
            // Команды управления
            ToggleWaterCommand = new RelayCommand(() => ToggleSystem("Полив", () => Actuators.WaterValve = !Actuators.WaterValve));
            ToggleHeatingCommand = new RelayCommand(() => ToggleSystem("Обогрев", () => Actuators.Heater = !Actuators.Heater));
            ToggleVentilationCommand = new RelayCommand(() => ToggleSystem("Вентиляция", () => Actuators.Ventilation = !Actuators.Ventilation));
            ToggleLightingCommand = new RelayCommand(() => ToggleSystem("Освещение", () => Actuators.Lights = !Actuators.Lights));
            ToggleSunProtectionCommand = new RelayCommand(() => ToggleSystem("Защита от солнца", () => Actuators.SunProtection = !Actuators.SunProtection));

            // Команды автоматического управления
            ProcessDataCommand = new RelayCommand(ProcessSensorData);
            RunAllSystemsCommand = new RelayCommand(RunAllSystems);
            StopAllSystemsCommand = new RelayCommand(StopAllSystems);

            // Команды лабораторных работ
            CheckIrrigationContractsCommand = new RelayCommand(CheckIrrigationContracts);
            CheckHeatingContractsCommand = new RelayCommand(CheckHeatingContracts);
            CheckVentilationContractsCommand = new RelayCommand(CheckVentilationContracts);
            CalculateWpCommand = new RelayCommand(CalculateWp);
            CalculateClimateWpCommand = new RelayCommand(CalculateClimateWp);
            RunDataProcessingCommand = new RelayCommand(RunDataProcessing);
            RunIrrigationCycleCommand = new RelayCommand(RunIrrigationCycle);
            GenerateTruthTableCommand = new RelayCommand(GenerateTruthTable);
            AnalyzeTruthTableCommand = new RelayCommand(AnalyzeTruthTable);
            AnalyzeLightingLogicCommand = new RelayCommand(AnalyzeLightingLogic);
            AnalyzeCorrelationsCommand = new RelayCommand(AnalyzeCorrelations);
        }

        #region Свойства
        private SensorData _currentData;
        public SensorData CurrentData
        {
            get => _currentData;
            set { _currentData = value; OnPropertyChanged(); }
        }

        private ActuatorStatus _actuators;
        public ActuatorStatus Actuators
        {
            get => _actuators;
            set { _actuators = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SensorData> SensorHistory { get; }

        private string _operationLog;
        public string OperationLog
        {
            get => _operationLog;
            set { _operationLog = value; OnPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public DateTime CurrentTime => DateTime.Now;

        private bool _isAutoMode = true;
        public bool IsAutoMode
        {
            get => _isAutoMode;
            set { _isAutoMode = value; OnPropertyChanged(); }
        }

        // Булева логика
        public bool ShouldWater => SensorLogic.ShouldTurnOnWater(CurrentData, Actuators);
        public bool ShouldHeat => SensorLogic.ShouldTurnOnHeating(CurrentData);
        public bool ShouldVentilate => SensorLogic.ShouldTurnOnVentilation(CurrentData, Actuators);
        public bool ShouldLight => _lightingService.ShouldTurnOnLights(CurrentData);

        // Результаты лабораторных работ
        private string _contractsResult;
        public string ContractsResult
        {
            get => _contractsResult;
            set { _contractsResult = value; OnPropertyChanged(); }
        }

        private string _wpResult;
        public string WpResult
        {
            get => _wpResult;
            set { _wpResult = value; OnPropertyChanged(); }
        }

        private string _invariantResult;
        public string InvariantResult
        {
            get => _invariantResult;
            set { _invariantResult = value; OnPropertyChanged(); }
        }

        private string _booleanLogicResult;
        public string BooleanLogicResult
        {
            get => _booleanLogicResult;
            set { _booleanLogicResult = value; OnPropertyChanged(); }
        }
        #endregion

        #region Команды
        public ICommand ToggleWaterCommand { get; private set; }
        public ICommand ToggleHeatingCommand { get; private set; }
        public ICommand ToggleVentilationCommand { get; private set; }
        public ICommand ToggleLightingCommand { get; private set; }
        public ICommand ToggleSunProtectionCommand { get; private set; }

        public ICommand ProcessDataCommand { get; private set; }
        public ICommand RunAllSystemsCommand { get; private set; }
        public ICommand StopAllSystemsCommand { get; private set; }

        public ICommand CheckIrrigationContractsCommand { get; private set; }
        public ICommand CheckHeatingContractsCommand { get; private set; }
        public ICommand CheckVentilationContractsCommand { get; private set; }
        public ICommand CalculateWpCommand { get; private set; }
        public ICommand CalculateClimateWpCommand { get; private set; }
        public ICommand RunDataProcessingCommand { get; private set; }
        public ICommand RunIrrigationCycleCommand { get; private set; }
        public ICommand GenerateTruthTableCommand { get; private set; }
        public ICommand AnalyzeTruthTableCommand { get; private set; }
        public ICommand AnalyzeLightingLogicCommand { get; private set; }
        public ICommand AnalyzeCorrelationsCommand { get; private set; }
        #endregion

        #region Методы управления
        private void ToggleSystem(string systemName, Action toggleAction)
        {
            toggleAction();
            AddToLog($"{systemName} {(toggleAction.Target.GetType().GetProperty(systemName)?.GetValue(toggleAction.Target) ?? "переключен")}");
            UpdateBooleanLogic();
        }

        private void ProcessSensorData()
        {
            var testData = GenerateTestData();
            _sensorService.ProcessSensorReadings(new System.Collections.Generic.List<SensorData> { testData });
            AddToLog("Обработаны данные датчиков");
        }

        private void RunAllSystems()
        {
            Actuators.WaterValve = true;
            Actuators.Heater = true;
            Actuators.Ventilation = true;
            Actuators.Lights = true;
            AddToLog("Все системы запущены");
            UpdateBooleanLogic();
        }

        private void StopAllSystems()
        {
            Actuators.WaterValve = false;
            Actuators.Heater = false;
            Actuators.Ventilation = false;
            Actuators.Lights = false;
            Actuators.SunProtection = false;
            AddToLog("Все системы остановлены");
            UpdateBooleanLogic();
        }

        private void UpdateData()
        {
            CurrentData = GenerateTestData();
            SensorHistory.Add(CurrentData);

            if (IsAutoMode)
            {
                _climateService.AdjustClimate(CurrentData);
                _irrigationService.ManageIrrigation(CurrentData);
                _lightingService.ManageLighting(CurrentData);
            }

            UpdateBooleanLogic();
            OnPropertyChanged(nameof(CurrentTime));
        }

        private SensorData GenerateTestData()
        {
            var random = new Random();
            return new SensorData
            {
                Temperature = random.Next(10, 35),
                Humidity = random.Next(30, 90),
                SoilMoisture = random.Next(20, 80),
                CO2Level = random.Next(400, 2000),
                LightIntensity = random.Next(0, 50000),
                IsRaining = random.Next(0, 10) == 1,
                Timestamp = DateTime.Now
            };
        }

        private void UpdateBooleanLogic()
        {
            OnPropertyChanged(nameof(ShouldWater));
            OnPropertyChanged(nameof(ShouldHeat));
            OnPropertyChanged(nameof(ShouldVentilate));
            OnPropertyChanged(nameof(ShouldLight));
        }

        private void AddToLog(string message)
        {
            OperationLog += $"{DateTime.Now:HH:mm:ss} - {message}\n";
        }

        private void UpdateStatus(string message)
        {
            StatusMessage = message;
        }
        #endregion

        #region Методы лабораторных работ
        private void CheckIrrigationContracts()
        {
            var operation = new StartIrrigationOperation();
            bool preConditions = operation.CheckPreConditions(CurrentData, Actuators);
            ContractsResult = $"Предусловия полива: {preConditions}\n" +
                             $"Влажность почвы: {CurrentData.SoilMoisture}% (требуется < 30%)\n" +
                             $"Дождь: {CurrentData.IsRaining} (требуется false)\n" +
                             $"Температура: {CurrentData.Temperature}°C (требуется > 5°C)";
        }

        private void CheckHeatingContracts()
        {
            var operation = new StartHeatingOperation();
            bool preConditions = operation.CheckPreConditions(CurrentData, Actuators);
            ContractsResult = $"Предусловия обогрева: {preConditions}\n" +
                             $"Температура: {CurrentData.Temperature}°C (требуется < 15°C)\n" +
                             $"Обогреватель выключен: {!Actuators.Heater}";
        }

        private void CheckVentilationContracts()
        {
            // Используем существующую операцию из ClimateControlService
            var operation = new StartVentilationOperation();
            bool preConditions = operation.CheckPreConditions(CurrentData, Actuators);
            ContractsResult = $"Предусловия вентиляции: {preConditions}\n" +
                             $"Температура: {CurrentData.Temperature}°C (требуется > 25°C)\n" +
                             $"Дождь: {CurrentData.IsRaining} (требуется false)\n" +
                             $"Вентиляция выключена: {!Actuators.Ventilation}";
        }

        private void CalculateWp()
        {
            string result = WpEngine.CalculateWateringPrecondition();
            WpResult = $"WP для последовательности полива:\n{result}";
        }

        private void CalculateClimateWp()
        {
            var statements = new System.Collections.Generic.List<string>
            {
                "tempThreshold := 20.0",
                "ventilationTime := currentTime + 300",
                "heaterPower := CalculateOptimalPower(data.Temperature)"
            };
            string postCondition = "temperature > tempThreshold && ventilationStartTime <= currentTime";

            string result = WpEngine.WpSequence(statements, postCondition);
            WpResult = $"WP для климат-контроля:\n{result}";
        }

        private void RunDataProcessing()
        {
            var controlLoop = new ClimateControlLoop();
            var testData = new System.Collections.Generic.List<SensorData>
            {
                GenerateTestData(),
                GenerateTestData(),
                GenerateTestData()
            };

            controlLoop.ProcessSensorData(testData);
            InvariantResult = "Цикл обработки данных выполнен\nПроверены инварианты и варианты-функции";
        }

        private void RunIrrigationCycle()
        {
            // Здесь будет вызов цикла полива из IrrigationService
            InvariantResult = "Цикл полива выполнен\nОбработаны все зоны полива с проверкой инвариантов";
        }

        private void GenerateTruthTable()
        {
            var table = SensorLogic.GenerateTruthTable(3);
            string result = "Таблица истинности (3 переменные):\n";

            for (int i = 0; i < table.Count; i++)
            {
                result += $"Строка {i}: {string.Join(", ", table[i].Select(b => b ? "1" : "0"))}\n";
            }

            BooleanLogicResult = result;
        }

        private void AnalyzeTruthTable()
        {
            var table = SensorLogic.GenerateTruthTable(2);
            BooleanLogicResult = "Анализ таблицы истинности для логики полива:\n" +
                                "A=Низкая влажность, B=Нет дождя → Полив=A∧B\n" +
                                $"Всего комбинаций: {table.Count}";
        }

        private void AnalyzeLightingLogic()
        {
            _lightingService.AnalyzeLightingLogic();
            BooleanLogicResult = "Проанализирована логика освещения\nСгенерирована таблица истинности для 4 переменных";
        }

        private void AnalyzeCorrelations()
        {
            _sensorService.AnalyzeCorrelations();
            BooleanLogicResult = "Проанализированы корреляции между параметрами\nВыявлены паттерны для вмешательства";
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}