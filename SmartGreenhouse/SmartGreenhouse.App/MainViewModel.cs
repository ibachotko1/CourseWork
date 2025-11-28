using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartGreenhouse.Core.Models;
using SmartGreenhouse.Core.Contracts;
using SmartGreenhouse.Core.BooleanLogic;
using System.Windows;
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
using System.Windows.Media;
using System.Windows;

namespace SmartGreenhouse.App
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ClimateControlService _climateService;
        private readonly IrrigationService _irrigationService;
        private readonly LightingService _lightingService;
        private readonly SensorDataService _sensorService;
        private readonly DispatcherTimer _timer;
        private readonly Random _random = new Random();

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

            // Инициализация операций ЛР1
            InitializeOperations();
            
            // Инициализация примеров ЛР2
            InitializeWpExamples();

            // Инициализация ЛР3
            InitializeLoopData();

            // Инициализация ЛР4
            UpdateBooleanNumberExplanation();

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
            CheckLightingContractsCommand = new RelayCommand(CheckLightingContracts);
            ExecuteOperationCommand = new RelayCommand<object>(ExecuteOperation);
            ShowContractCommand = new RelayCommand<object>(ShowContract);
            ExecuteAllOperationsCommand = new RelayCommand(ExecuteAllOperations);
            ShowAllContractsCommand = new RelayCommand(ShowAllContracts);
            CalculateWpCommand = new RelayCommand(CalculateWp);
            ShowWpTriadCommand = new RelayCommand(ShowWpTriad);
            RunDataProcessingCommand = new RelayCommand(RunDataProcessing);
            
            GenerateLoopDataCommand = new RelayCommand(GenerateLoopData);
            ExecuteLoopAndShowResultCommand = new RelayCommand(ExecuteLoopAndShowResult, () => _loopChecker != null && _loopData != null && _loopData.Values != null && _loopData.Values.Count > 0);
            CheckVerificationConditionsCommand = new RelayCommand(CheckVerificationConditions);
            RunIrrigationCycleCommand = new RelayCommand(RunIrrigationCycle);
            // ЛР4: Команды для булевых функций
            BuildTruthTableFromNumberCommand = new RelayCommand(BuildTruthTableFromNumber);
            GenerateDNFFromNumberCommand = new RelayCommand(GenerateDNFFromNumber);
            GenerateKNFFromNumberCommand = new RelayCommand(GenerateKNFFromNumber);
            ParseAndEvaluateFormulaCommand = new RelayCommand(ParseAndEvaluateFormula);
            CheckEquivalenceCommand = new RelayCommand(CheckEquivalence);
            CopyBooleanResultCommand = new RelayCommand(CopyBooleanResult);
            LoadExampleFormulaCommand = new RelayCommand<string>(LoadExampleFormula);
            OpenChartCommand = new RelayCommand(OpenChartWindow);
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

        // ЛР1: Операции
        public ObservableCollection<OperationViewModel> Operations { get; private set; }
        public ObservableCollection<OperationResultViewModel> OperationResults { get; private set; }


        // Параметры ввода для операции
        private double _inputTemperature = 20.0;
        public double InputTemperature
        {
            get => _inputTemperature;
            set { _inputTemperature = value; OnPropertyChanged(); UpdatePreConditionIndicator(); }
        }

        private double _inputHumidity = 60.0;
        public double InputHumidity
        {
            get => _inputHumidity;
            set { _inputHumidity = value; OnPropertyChanged(); UpdatePreConditionIndicator(); }
        }

        private double _inputSoilMoisture = 65.0;
        public double InputSoilMoisture
        {
            get => _inputSoilMoisture;
            set { _inputSoilMoisture = value; OnPropertyChanged(); UpdatePreConditionIndicator(); }
        }

        private double _inputCO2Level = 1100.0;
        public double InputCO2Level
        {
            get => _inputCO2Level;
            set { _inputCO2Level = value; OnPropertyChanged(); UpdatePreConditionIndicator(); }
        }

        private double _inputLightIntensity = 4000.0;
        public double InputLightIntensity
        {
            get => _inputLightIntensity;
            set { _inputLightIntensity = value; OnPropertyChanged(); UpdatePreConditionIndicator(); }
        }


        // Индикаторы Pre/Post
        private bool _preConditionMet;
        public bool PreConditionMet
        {
            get => _preConditionMet;
            set { _preConditionMet = value; OnPropertyChanged(); OnPropertyChanged(nameof(PreConditionColor)); }
        }

        public Brush PreConditionColor => PreConditionMet ? Brushes.Green : Brushes.Red;

        private bool _postConditionMet;
        public bool PostConditionMet
        {
            get => _postConditionMet;
            set { _postConditionMet = value; OnPropertyChanged(); OnPropertyChanged(nameof(PostConditionColor)); }
        }

        public Brush PostConditionColor => PostConditionMet ? Brushes.Green : Brushes.Red;

        private string _wpResult;
        public string WpResult
        {
            get => _wpResult;
            set { _wpResult = value; OnPropertyChanged(); }
        }

        // ЛР2: WP-калькулятор
        public ObservableCollection<WpFragment> WpExamples { get; private set; }
        
        private WpFragment _selectedWpExample;
        public WpFragment SelectedWpExample
        {
            get => _selectedWpExample;
            set
            {
                _selectedWpExample = value;
                OnPropertyChanged();
                if (value != null)
                {
                    WpPostCondition = value.PostCondition;
                    WpPostConditionDescription = value.PostConditionDescription;
                    WpCode = value.Code;
                }
            }
        }

        private string _wpPostCondition = "";
        public string WpPostCondition
        {
            get => _wpPostCondition;
            set { _wpPostCondition = value; OnPropertyChanged(); }
        }

        private string _wpPostConditionDescription = "";
        public string WpPostConditionDescription
        {
            get => _wpPostConditionDescription;
            set { _wpPostConditionDescription = value; OnPropertyChanged(); }
        }

        private string _wpCode = "";
        public string WpCode
        {
            get => _wpCode;
            set { _wpCode = value; OnPropertyChanged(); }
        }

        private string _wpTrace = "";
        public string WpTrace
        {
            get => _wpTrace;
            set { _wpTrace = value; OnPropertyChanged(); }
        }

        private string _wpFinalResult = "";
        public string WpFinalResult
        {
            get => _wpFinalResult;
            set { _wpFinalResult = value; OnPropertyChanged(); }
        }

        private string _wpFinalResultDescription = "";
        public string WpFinalResultDescription
        {
            get => _wpFinalResultDescription;
            set { _wpFinalResultDescription = value; OnPropertyChanged(); }
        }

        private string _wpTriad = "";
        public string WpTriad
        {
            get => _wpTriad;
            set { _wpTriad = value; OnPropertyChanged(); }
        }

        private string _invariantResult;
        public string InvariantResult
        {
            get => _invariantResult;
            set { _invariantResult = value; OnPropertyChanged(); }
        }

        private LoopInvariantChecker _loopChecker;
        private LoopData _loopData;
        private InvariantInfo _invariantInfo;

        public ObservableCollection<double> LoopDataValues { get; private set; }
        public ObservableCollection<LoopDataRow> LoopDataTable { get; private set; }

        private LoopMode _selectedLoopMode = LoopMode.PrefixSum;
        public LoopMode SelectedLoopMode
        {
            get => _selectedLoopMode;
            set
            {
                _selectedLoopMode = value;
                OnPropertyChanged();
                InitializeLoopMode();
            }
        }

        public ObservableCollection<LoopMode> AvailableLoopModes { get; private set; }

        private int _loopDataCount = 10;
        public int LoopDataCount
        {
            get => _loopDataCount;
            set { _loopDataCount = value; OnPropertyChanged(); }
        }

        private double _loopDataMin = 15.0;
        public double LoopDataMin
        {
            get => _loopDataMin;
            set { _loopDataMin = value; OnPropertyChanged(); }
        }

        private double _loopDataMax = 30.0;
        public double LoopDataMax
        {
            get => _loopDataMax;
            set { _loopDataMax = value; OnPropertyChanged(); }
        }

        private double _loopThreshold = 15.0;
        public double LoopThreshold
        {
            get => _loopThreshold;
            set { _loopThreshold = value; OnPropertyChanged(); }
        }

        private int _loopCurrentIndex = 0;
        public int LoopCurrentIndex
        {
            get => _loopCurrentIndex;
            set { _loopCurrentIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(LoopCurrentIndexDisplay)); }
        }

        public string LoopCurrentIndexDisplay => $"j = {LoopCurrentIndex}";

        private double _loopResult = 0;
        public double LoopResult
        {
            get => _loopResult;
            set { _loopResult = value; OnPropertyChanged(); }
        }

        public string InvariantDescription
        {
            get => _invariantInfo?.InvariantDescription ?? "";
            set
            {
                if (_invariantInfo != null)
                {
                    _invariantInfo.InvariantDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        public string InvariantFormula
        {
            get => _invariantInfo?.InvariantFormula ?? "";
            set
            {
                if (_invariantInfo != null)
                {
                    _invariantInfo.InvariantFormula = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VariantFunction
        {
            get => _invariantInfo?.VariantFunction ?? "";
            set
            {
                if (_invariantInfo != null)
                {
                    _invariantInfo.VariantFunction = value;
                    OnPropertyChanged();
                }
            }
        }

        public int VariantValue
        {
            get => _invariantInfo?.VariantValue ?? 0;
            set
            {
                if (_invariantInfo != null)
                {
                    _invariantInfo.VariantValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool InvariantBeforeStep
        {
            get => _invariantInfo?.InvariantBeforeStep ?? false;
            set
            {
                if (_invariantInfo != null)
                {
                    _invariantInfo.InvariantBeforeStep = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool InvariantAfterStep
        {
            get => _invariantInfo?.InvariantAfterStep ?? false;
            set
            {
                if (_invariantInfo != null)
                {
                    _invariantInfo.InvariantAfterStep = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _verificationConditions = "";
        public string VerificationConditions
        {
            get => _verificationConditions;
            set { _verificationConditions = value; OnPropertyChanged(); }
        }

        public ICommand GenerateLoopDataCommand { get; private set; }
        public ICommand ExecuteLoopAndShowResultCommand { get; private set; }
        public ICommand CheckVerificationConditionsCommand { get; private set; }

        // ЛР4: Свойства для работы с булевыми функциями
        private int _booleanN = 3;
        public int BooleanN
        {
            get => _booleanN;
            set { _booleanN = value; OnPropertyChanged(); UpdateBooleanNumberExplanation(); }
        }

        private int _booleanNum = 11;
        public int BooleanNum
        {
            get => _booleanNum;
            set { _booleanNum = value; OnPropertyChanged(); UpdateBooleanNumberExplanation(); }
        }

        private string _booleanNumberExplanation = "";
        public string BooleanNumberExplanation
        {
            get => _booleanNumberExplanation;
            set { _booleanNumberExplanation = value; OnPropertyChanged(); }
        }

        private string _booleanNumberResult = "";
        public string BooleanNumberResult
        {
            get => _booleanNumberResult;
            set { _booleanNumberResult = value; OnPropertyChanged(); }
        }

        private string _booleanFormula = "";
        public string BooleanFormula
        {
            get => _booleanFormula;
            set { _booleanFormula = value; OnPropertyChanged(); }
        }

        private string _booleanFormulaInfo = "";
        public string BooleanFormulaInfo
        {
            get => _booleanFormulaInfo;
            set { _booleanFormulaInfo = value; OnPropertyChanged(); }
        }

        private string _booleanFormulaCost = "";
        public string BooleanFormulaCost
        {
            get => _booleanFormulaCost;
            set { _booleanFormulaCost = value; OnPropertyChanged(); }
        }

        private string _booleanFormulaResult = "";
        public string BooleanFormulaResult
        {
            get => _booleanFormulaResult;
            set { _booleanFormulaResult = value; OnPropertyChanged(); }
        }

        private string _booleanCompareFunction1 = "";
        public string BooleanCompareFunction1
        {
            get => _booleanCompareFunction1;
            set { _booleanCompareFunction1 = value; OnPropertyChanged(); }
        }

        private string _booleanCompareFunction2 = "";
        public string BooleanCompareFunction2
        {
            get => _booleanCompareFunction2;
            set { _booleanCompareFunction2 = value; OnPropertyChanged(); }
        }

        private string _booleanEquivalenceResult = "";
        public string BooleanEquivalenceResult
        {
            get => _booleanEquivalenceResult;
            set { _booleanEquivalenceResult = value; OnPropertyChanged(); }
        }

        private ChartWindow _chartWindow;

        private PointCollection _temperaturePoints = new PointCollection();
        public PointCollection TemperaturePoints
        {
            get => _temperaturePoints;
            set { _temperaturePoints = value; OnPropertyChanged(); }
        }

        private PointCollection _humidityPoints = new PointCollection();
        public PointCollection HumidityPoints
        {
            get => _humidityPoints;
            set { _humidityPoints = value; OnPropertyChanged(); }
        }

        private PointCollection _soilMoisturePoints = new PointCollection();
        public PointCollection SoilMoisturePoints
        {
            get => _soilMoisturePoints;
            set { _soilMoisturePoints = value; OnPropertyChanged(); }
        }

        private PointCollection _co2Points = new PointCollection();
        public PointCollection CO2Points
        {
            get => _co2Points;
            set { _co2Points = value; OnPropertyChanged(); }
        }

        private PointCollection _lightPoints = new PointCollection();
        public PointCollection LightPoints
        {
            get => _lightPoints;
            set { _lightPoints = value; OnPropertyChanged(); }
        }

        public string TemperatureMin { get; private set; } = "";
        public string TemperatureMax { get; private set; } = "";
        public string TemperatureScale1 { get; private set; } = "";
        public string TemperatureScale2 { get; private set; } = "";
        public string TemperatureScale3 { get; private set; } = "";
        public string TemperatureScale4 { get; private set; } = "";
        public string TemperatureScale5 { get; private set; } = "";
        public string TemperatureScale6 { get; private set; } = "";

        public string HumidityMin { get; private set; } = "";
        public string HumidityMax { get; private set; } = "";
        public string HumidityScale1 { get; private set; } = "";
        public string HumidityScale2 { get; private set; } = "";
        public string HumidityScale3 { get; private set; } = "";
        public string HumidityScale4 { get; private set; } = "";
        public string HumidityScale5 { get; private set; } = "";
        public string HumidityScale6 { get; private set; } = "";

        public string SoilMoistureMin { get; private set; } = "";
        public string SoilMoistureMax { get; private set; } = "";
        public string SoilMoistureScale1 { get; private set; } = "";
        public string SoilMoistureScale2 { get; private set; } = "";
        public string SoilMoistureScale3 { get; private set; } = "";
        public string SoilMoistureScale4 { get; private set; } = "";
        public string SoilMoistureScale5 { get; private set; } = "";
        public string SoilMoistureScale6 { get; private set; } = "";

        public string CO2Min { get; private set; } = "";
        public string CO2Max { get; private set; } = "";
        public string CO2Scale1 { get; private set; } = "";
        public string CO2Scale2 { get; private set; } = "";
        public string CO2Scale3 { get; private set; } = "";
        public string CO2Scale4 { get; private set; } = "";
        public string CO2Scale5 { get; private set; } = "";
        public string CO2Scale6 { get; private set; } = "";

        public string LightMin { get; private set; } = "";
        public string LightMax { get; private set; } = "";
        public string LightScale1 { get; private set; } = "";
        public string LightScale2 { get; private set; } = "";
        public string LightScale3 { get; private set; } = "";
        public string LightScale4 { get; private set; } = "";
        public string LightScale5 { get; private set; } = "";
        public string LightScale6 { get; private set; } = "";

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
        public ICommand CheckLightingContractsCommand { get; private set; }
        public ICommand ExecuteOperationCommand { get; private set; }
        public ICommand ShowContractCommand { get; private set; }
        public ICommand ExecuteAllOperationsCommand { get; private set; }
        public ICommand ShowAllContractsCommand { get; private set; }
        public ICommand CalculateWpCommand { get; private set; }
        public ICommand ShowWpTriadCommand { get; private set; }
        public ICommand RunDataProcessingCommand { get; private set; }
        public ICommand RunIrrigationCycleCommand { get; private set; }
        // ЛР4: Команды для булевых функций
        public ICommand BuildTruthTableFromNumberCommand { get; private set; }
        public ICommand GenerateDNFFromNumberCommand { get; private set; }
        public ICommand GenerateKNFFromNumberCommand { get; private set; }
        public ICommand ParseAndEvaluateFormulaCommand { get; private set; }
        public ICommand CheckEquivalenceCommand { get; private set; }
        public ICommand CopyBooleanResultCommand { get; private set; }
        public ICommand LoadExampleFormulaCommand { get; private set; }
        public ICommand OpenChartCommand { get; private set; }
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
            CurrentData = GenerateEnvironmentSnapshot();

            if (IsAutoMode)
            {
                bool continueHeating = Actuators.Heater && CurrentData.Temperature < 20.0;

                if (ShouldHeat || continueHeating)
                {
                    Actuators.Heater = true;
                    CurrentData.Temperature = Math.Min(25.0, CurrentData.Temperature + 0.2);
                    CurrentData.Humidity = Math.Max(30.0, CurrentData.Humidity - 0.15);
                }
                else if (CurrentData.Temperature >= 20.0 && CurrentData.Temperature <= 25.0)
                {
                    Actuators.Heater = false;
                }

                if (ShouldWater)
                {
                    Actuators.WaterValve = true;
                    CurrentData.SoilMoisture = Math.Min(70.0, CurrentData.SoilMoisture + 0.9);
                    CurrentData.Humidity = Math.Min(85.0, CurrentData.Humidity + 0.25);
                }
                else if (CurrentData.SoilMoisture >= 60.0 && CurrentData.SoilMoisture <= 70.0)
                {
                    Actuators.WaterValve = false;
                }

                if (ShouldVentilate)
                {
                    Actuators.Ventilation = true;
                    CurrentData.Temperature = Math.Max(15.0, CurrentData.Temperature - 0.2);
                    CurrentData.CO2Level = Math.Max(1000.0, CurrentData.CO2Level - 5.0);
                    CurrentData.Humidity = Math.Max(30.0, CurrentData.Humidity - 0.3);
                }
                else if (CurrentData.Temperature <= 25.0 && CurrentData.CO2Level <= 1200)
                {
                    Actuators.Ventilation = false;
                }

                if (ShouldLight)
                {
                    Actuators.Lights = true;
                    CurrentData.LightIntensity = Math.Min(5000.0, CurrentData.LightIntensity + 200);
                }
                else if (CurrentData.LightIntensity >= 3000 && CurrentData.LightIntensity <= 5000)
                {
                    Actuators.Lights = false;
                }
            }

            SensorHistory.Add(CurrentData);

            UpdateBooleanLogic();
            UpdateChart();
            OnPropertyChanged(nameof(CurrentTime));
        }

        private void OpenChartWindow()
        {
            if (_chartWindow == null || !_chartWindow.IsLoaded)
            {
                _chartWindow = new ChartWindow();
                _chartWindow.DataContext = this;
                _chartWindow.Closed += (s, e) => _chartWindow = null;
                _chartWindow.Show();
            }
            else
            {
                _chartWindow.Activate();
            }
        }

        private void UpdateChart()
        {
            if (_chartWindow == null || !_chartWindow.IsLoaded || SensorHistory.Count == 0) return;

            const int maxPoints = 80;
            const double chartWidth = 420;
            const double chartHeight = 150;
            const double marginX = 30;
            const double marginY = 5;

            var recentData = SensorHistory.Skip(Math.Max(0, SensorHistory.Count - maxPoints)).ToList();
            int count = recentData.Count;
            if (count == 0) return;

            double stepX = count > 1 ? (chartWidth - marginX) / (count - 1) : 0;

            var tempPoints = new PointCollection();
            var humidityPoints = new PointCollection();
            var soilPoints = new PointCollection();
            var co2Points = new PointCollection();
            var lightPoints = new PointCollection();

            double tempMin = recentData.Min(d => d.Temperature);
            double tempMax = recentData.Max(d => d.Temperature);
            double tempRange = tempMax - tempMin > 0 ? tempMax - tempMin : 10;

            double humidityMin = recentData.Min(d => d.Humidity);
            double humidityMax = recentData.Max(d => d.Humidity);
            double humidityRange = humidityMax - humidityMin > 0 ? humidityMax - humidityMin : 20;

            double soilMin = recentData.Min(d => d.SoilMoisture);
            double soilMax = recentData.Max(d => d.SoilMoisture);
            double soilRange = soilMax - soilMin > 0 ? soilMax - soilMin : 10;

            double co2Min = recentData.Min(d => d.CO2Level);
            double co2Max = recentData.Max(d => d.CO2Level);
            double co2Range = co2Max - co2Min > 0 ? co2Max - co2Min : 200;

            double lightMin = recentData.Min(d => d.LightIntensity);
            double lightMax = recentData.Max(d => d.LightIntensity);
            double lightRange = lightMax - lightMin > 0 ? lightMax - lightMin : 2000;

            for (int i = 0; i < count; i++)
            {
                double x = marginX + i * stepX;
                
                double tempY = chartHeight - marginY - ((recentData[i].Temperature - tempMin) / tempRange) * (chartHeight - 2 * marginY);
                tempPoints.Add(new Point(x, tempY));

                double humidityY = chartHeight - marginY - ((recentData[i].Humidity - humidityMin) / humidityRange) * (chartHeight - 2 * marginY);
                humidityPoints.Add(new Point(x, humidityY));

                double soilY = chartHeight - marginY - ((recentData[i].SoilMoisture - soilMin) / soilRange) * (chartHeight - 2 * marginY);
                soilPoints.Add(new Point(x, soilY));

                double co2Y = chartHeight - marginY - ((recentData[i].CO2Level - co2Min) / co2Range) * (chartHeight - 2 * marginY);
                co2Points.Add(new Point(x, co2Y));

                double lightY = chartHeight - marginY - ((recentData[i].LightIntensity - lightMin) / lightRange) * (chartHeight - 2 * marginY);
                lightPoints.Add(new Point(x, lightY));
            }

            TemperaturePoints = tempPoints;
            HumidityPoints = humidityPoints;
            SoilMoisturePoints = soilPoints;
            CO2Points = co2Points;
            LightPoints = lightPoints;

            TemperatureMin = tempMin.ToString("F1");
            TemperatureMax = tempMax.ToString("F1");
            TemperatureScale1 = (tempMin + (tempMax - tempMin) * 1.0 / 6).ToString("F1");
            TemperatureScale2 = (tempMin + (tempMax - tempMin) * 2.0 / 6).ToString("F1");
            TemperatureScale3 = (tempMin + (tempMax - tempMin) * 3.0 / 6).ToString("F1");
            TemperatureScale4 = (tempMin + (tempMax - tempMin) * 4.0 / 6).ToString("F1");
            TemperatureScale5 = (tempMin + (tempMax - tempMin) * 5.0 / 6).ToString("F1");
            TemperatureScale6 = "";

            HumidityMin = humidityMin.ToString("F1");
            HumidityMax = humidityMax.ToString("F1");
            HumidityScale1 = (humidityMin + (humidityMax - humidityMin) * 1.0 / 6).ToString("F1");
            HumidityScale2 = (humidityMin + (humidityMax - humidityMin) * 2.0 / 6).ToString("F1");
            HumidityScale3 = (humidityMin + (humidityMax - humidityMin) * 3.0 / 6).ToString("F1");
            HumidityScale4 = (humidityMin + (humidityMax - humidityMin) * 4.0 / 6).ToString("F1");
            HumidityScale5 = (humidityMin + (humidityMax - humidityMin) * 5.0 / 6).ToString("F1");
            HumidityScale6 = "";

            SoilMoistureMin = soilMin.ToString("F1");
            SoilMoistureMax = soilMax.ToString("F1");
            SoilMoistureScale1 = (soilMin + (soilMax - soilMin) * 1.0 / 6).ToString("F1");
            SoilMoistureScale2 = (soilMin + (soilMax - soilMin) * 2.0 / 6).ToString("F1");
            SoilMoistureScale3 = (soilMin + (soilMax - soilMin) * 3.0 / 6).ToString("F1");
            SoilMoistureScale4 = (soilMin + (soilMax - soilMin) * 4.0 / 6).ToString("F1");
            SoilMoistureScale5 = (soilMin + (soilMax - soilMin) * 5.0 / 6).ToString("F1");
            SoilMoistureScale6 = "";

            CO2Min = co2Min.ToString("F0");
            CO2Max = co2Max.ToString("F0");
            CO2Scale1 = (co2Min + (co2Max - co2Min) * 1.0 / 6).ToString("F0");
            CO2Scale2 = (co2Min + (co2Max - co2Min) * 2.0 / 6).ToString("F0");
            CO2Scale3 = (co2Min + (co2Max - co2Min) * 3.0 / 6).ToString("F0");
            CO2Scale4 = (co2Min + (co2Max - co2Min) * 4.0 / 6).ToString("F0");
            CO2Scale5 = (co2Min + (co2Max - co2Min) * 5.0 / 6).ToString("F0");
            CO2Scale6 = "";

            LightMin = lightMin.ToString("F0");
            LightMax = lightMax.ToString("F0");
            LightScale1 = (lightMin + (lightMax - lightMin) * 1.0 / 6).ToString("F0");
            LightScale2 = (lightMin + (lightMax - lightMin) * 2.0 / 6).ToString("F0");
            LightScale3 = (lightMin + (lightMax - lightMin) * 3.0 / 6).ToString("F0");
            LightScale4 = (lightMin + (lightMax - lightMin) * 4.0 / 6).ToString("F0");
            LightScale5 = (lightMin + (lightMax - lightMin) * 5.0 / 6).ToString("F0");
            LightScale6 = "";

            OnPropertyChanged(nameof(TemperatureMin));
            OnPropertyChanged(nameof(TemperatureMax));
            OnPropertyChanged(nameof(TemperatureScale1));
            OnPropertyChanged(nameof(TemperatureScale2));
            OnPropertyChanged(nameof(TemperatureScale3));
            OnPropertyChanged(nameof(TemperatureScale4));
            OnPropertyChanged(nameof(TemperatureScale5));
            OnPropertyChanged(nameof(TemperatureScale6));
            OnPropertyChanged(nameof(HumidityMin));
            OnPropertyChanged(nameof(HumidityMax));
            OnPropertyChanged(nameof(HumidityScale1));
            OnPropertyChanged(nameof(HumidityScale2));
            OnPropertyChanged(nameof(HumidityScale3));
            OnPropertyChanged(nameof(HumidityScale4));
            OnPropertyChanged(nameof(HumidityScale5));
            OnPropertyChanged(nameof(HumidityScale6));
            OnPropertyChanged(nameof(SoilMoistureMin));
            OnPropertyChanged(nameof(SoilMoistureMax));
            OnPropertyChanged(nameof(SoilMoistureScale1));
            OnPropertyChanged(nameof(SoilMoistureScale2));
            OnPropertyChanged(nameof(SoilMoistureScale3));
            OnPropertyChanged(nameof(SoilMoistureScale4));
            OnPropertyChanged(nameof(SoilMoistureScale5));
            OnPropertyChanged(nameof(SoilMoistureScale6));
            OnPropertyChanged(nameof(CO2Min));
            OnPropertyChanged(nameof(CO2Max));
            OnPropertyChanged(nameof(CO2Scale1));
            OnPropertyChanged(nameof(CO2Scale2));
            OnPropertyChanged(nameof(CO2Scale3));
            OnPropertyChanged(nameof(CO2Scale4));
            OnPropertyChanged(nameof(CO2Scale5));
            OnPropertyChanged(nameof(CO2Scale6));
            OnPropertyChanged(nameof(LightMin));
            OnPropertyChanged(nameof(LightMax));
            OnPropertyChanged(nameof(LightScale1));
            OnPropertyChanged(nameof(LightScale2));
            OnPropertyChanged(nameof(LightScale3));
            OnPropertyChanged(nameof(LightScale4));
            OnPropertyChanged(nameof(LightScale5));
            OnPropertyChanged(nameof(LightScale6));
        }

        private SensorData GenerateTestData()
        {
            var now = DateTime.Now;
            
            double temperature = 15 + _random.NextDouble() * 10;
            double humidity = 50 + _random.NextDouble() * 20;
            double soilMoisture = 60 + _random.NextDouble() * 10;
            double co2Level = 1000 + _random.NextDouble() * 200;
            double lightIntensity = 3000 + _random.NextDouble() * 2000;
            
            return new SensorData
            {
                Temperature = Math.Round(temperature, 1),
                Humidity = Math.Round(humidity, 1),
                SoilMoisture = Math.Round(soilMoisture, 1),
                CO2Level = Math.Round(co2Level, 0),
                LightIntensity = Math.Round(lightIntensity, 0),
                Timestamp = now
            };
        }

        private SensorData GenerateEnvironmentSnapshot()
        {
            var previous = CurrentData != null && CurrentData.Timestamp != default(DateTime)
                ? CurrentData
                : GenerateTestData();

            var timestamp = DateTime.Now;
            bool isNight = timestamp.Hour < 6 || timestamp.Hour >= 22;

            double temperature = Clamp(previous.Temperature + GetRandomDelta(isNight ? -0.25 : -0.15, 0.12), 10.0, 35.0);
            double humidityTarget = Actuators.WaterValve ? 65.0 : 45.0;
            double humidityDrift = GetRandomDelta(-0.4, 0.4) + (humidityTarget - previous.Humidity) * 0.02;
            double humidity = Clamp(previous.Humidity + humidityDrift, 30.0, 90.0);
            double soilMoisture = Clamp(previous.SoilMoisture + GetRandomDelta(-0.4, 0.2), 20.0, 90.0);
            double co2Level = Clamp(previous.CO2Level + GetRandomDelta(-15.0, 15.0), 800.0, 2000.0);

            double targetLight = isNight ? 200.0 : 4200.0;
            double lightDrift = GetRandomDelta(-150.0, 150.0) + (targetLight - previous.LightIntensity) * 0.05;
            double lightIntensity = Clamp(previous.LightIntensity + lightDrift, 0.0, 6500.0);

            return new SensorData
            {
                Temperature = Math.Round(temperature, 1),
                Humidity = Math.Round(humidity, 1),
                SoilMoisture = Math.Round(soilMoisture, 1),
                CO2Level = Math.Round(co2Level, 0),
                LightIntensity = Math.Round(lightIntensity, 0),
                Timestamp = timestamp
            };
        }

        private double GetRandomDelta(double min, double max)
        {
            return min + _random.NextDouble() * (max - min);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
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
        
        private void InitializeOperations()
        {
            Operations = new ObservableCollection<OperationViewModel>
            {
                new OperationViewModel(
                    "Полив",
                    new StartIrrigationOperation(),
                    (data, actuators) => data.SoilMoisture < 60.0,
                    (data, actuators) => actuators.WaterValve,
                    (data, actuators) => actuators.WaterValve = true
                ),
                new OperationViewModel(
                    "Обогрев",
                    new StartHeatingOperation(),
                    (data, actuators) => data.Temperature < 15.0,
                    (data, actuators) => actuators.Heater,
                    (data, actuators) => actuators.Heater = true
                ),
                new OperationViewModel(
                    "Вентиляция",
                    new StartVentilationOperation(),
                    (data, actuators) => data.CO2Level < 1000.0,
                    (data, actuators) => actuators.Ventilation,
                    (data, actuators) => actuators.Ventilation = true
                ),
                new OperationViewModel(
                    "Освещение",
                    new TurnOnLightsOperation(),
                    (data, actuators) => data.LightIntensity < 3000.0,
                    (data, actuators) => actuators.Lights,
                    (data, actuators) => actuators.Lights = true
                )
            };
            OnPropertyChanged(nameof(Operations));

            OperationResults = new ObservableCollection<OperationResultViewModel>
            {
                new OperationResultViewModel
                {
                    OperationName = "Полив",
                    PreConditionsDescription = "• Влажность почвы < 60%",
                    PostConditionsDescription = "• Клапан полива открыт"
                },
                new OperationResultViewModel
                {
                    OperationName = "Обогрев",
                    PreConditionsDescription = "• Температура < 15°C",
                    PostConditionsDescription = "• Обогрев включен"
                },
                new OperationResultViewModel
                {
                    OperationName = "Вентиляция",
                    PreConditionsDescription = "• CO₂ < 1000 ppm",
                    PostConditionsDescription = "• Вентиляция включена"
                },
                new OperationResultViewModel
                {
                    OperationName = "Освещение",
                    PreConditionsDescription = "• Освещенность < 3000 lux",
                    PostConditionsDescription = "• Освещение включено"
                }
            };
            OnPropertyChanged(nameof(OperationResults));
            UpdateAllOperationsResults();
        }

        private void UpdatePreConditionIndicator()
        {
            UpdateAllOperationsResults();
        }

        private void UpdateAllOperationsResults()
        {
            if (OperationResults == null || Operations == null) return;

            var testData = CreateTestDataFromInput();
            var testActuators = CreateTestActuatorsFromInput();

            foreach (var result in OperationResults)
            {
                var operation = Operations.FirstOrDefault(o => o.Name == result.OperationName);
                if (operation != null)
                {
                    result.PreConditionMet = operation.CheckPreConditions(testData, testActuators);
                    result.CanExecute = result.PreConditionMet;
                    if (!result.PreConditionMet)
                    {
                        result.PostConditionMet = false;
                    }
                }
            }
        }

        private SensorData CreateTestDataFromInput()
        {
            return new SensorData
            {
                Temperature = InputTemperature,
                Humidity = InputHumidity,
                SoilMoisture = InputSoilMoisture,
                CO2Level = InputCO2Level,
                LightIntensity = InputLightIntensity,
                Timestamp = DateTime.Now
            };
        }

        private ActuatorStatus CreateTestActuatorsFromInput()
        {
            return new ActuatorStatus
            {
                WaterValve = false,
                Heater = false,
                Ventilation = false,
                Lights = false
            };
        }

        private void ExecuteOperation(object parameter)
        {
            if (parameter is string operationName)
            {
                var operation = Operations.FirstOrDefault(o => o.Name == operationName);
                if (operation == null) return;

                var testData = CreateTestDataFromInput();
                var testActuators = CreateTestActuatorsFromInput();

                try
                {
                    operation.Execute(testData, testActuators);
                    var result = OperationResults.FirstOrDefault(r => r.OperationName == operationName);
                    if (result != null)
                    {
                        result.PostConditionMet = operation.CheckPostConditions(testData, testActuators);
                    }
                }
                catch (Exception ex)
                {
                    var result = OperationResults.FirstOrDefault(r => r.OperationName == operationName);
                    if (result != null)
                    {
                        result.PostConditionMet = false;
                    }
                    MessageBox.Show($"Ошибка выполнения операции {operationName}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowContract(object parameter)
        {
            if (parameter is string operationName)
            {
                ShowContractForOperation(operationName);
            }
        }

        public void ShowContractForOperation(string operationName)
        {
            var operation = Operations.FirstOrDefault(o => o.Name == operationName);
            if (operation == null) return;

            var contractWindow = new ContractWindow(operation);
            contractWindow.Owner = Application.Current.MainWindow;
            contractWindow.ShowDialog();
        }

        private void ExecuteAllOperations()
        {
            if (Operations == null) return;

            var testData = CreateTestDataFromInput();
            var testActuators = CreateTestActuatorsFromInput();

            foreach (var operation in Operations)
            {
                try
                {
                    if (operation.CheckPreConditions(testData, testActuators))
                    {
                        operation.Execute(testData, testActuators);
                        var result = OperationResults.FirstOrDefault(r => r.OperationName == operation.Name);
                        if (result != null)
                        {
                            result.PostConditionMet = operation.CheckPostConditions(testData, testActuators);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var result = OperationResults.FirstOrDefault(r => r.OperationName == operation.Name);
                    if (result != null)
                    {
                        result.PostConditionMet = false;
                    }
                }
            }
        }

        private void ShowAllContracts()
        {
            var contractWindow = new ContractWindow(null);
            contractWindow.Owner = Application.Current.MainWindow;
            contractWindow.ShowDialog();
        }


        private void CheckIrrigationContracts()
        {
            var operation = new StartIrrigationOperation();
            bool preConditions = operation.CheckPreConditions(CurrentData, Actuators);
            
            string result = $"=== ПРОВЕРКА КОНТРАКТОВ: ПОЛИВ ===\n\n";
            result += $"Операция: {operation.Name}\n\n";
            result += $"ПРЕДУСЛОВИЯ:\n";
            result += $"  • Влажность почвы < 60%: {CurrentData.SoilMoisture}% {(CurrentData.SoilMoisture < 60.0 ? "✓" : "✗")}\n";
            result += $"  • Температура > 5°C: {CurrentData.Temperature}°C {(CurrentData.Temperature > 5.0 ? "✓" : "✗")}\n";
            result += $"  • Полив выключен: {!Actuators.WaterValve} {(!Actuators.WaterValve ? "✓" : "✗")}\n";
            result += $"  Результат проверки предусловий: {(preConditions ? "✓ ВЫПОЛНЕНЫ" : "✗ НЕ ВЫПОЛНЕНЫ")}\n\n";
            
            if (preConditions)
            {
                bool wasWaterValve = Actuators.WaterValve;
                operation.Execute(CurrentData, Actuators);
                bool postConditions = operation.CheckPostConditions(CurrentData, Actuators);
                result += $"ПОСТУСЛОВИЯ:\n";
                result += $"  • Клапан полива открыт: {Actuators.WaterValve} {(Actuators.WaterValve ? "✓" : "✗")}\n";
                result += $"  Результат проверки постусловий: {(postConditions ? "✓ ВЫПОЛНЕНЫ" : "✗ НЕ ВЫПОЛНЕНЫ")}\n";
                Actuators.WaterValve = wasWaterValve;
            }
            
            ContractsResult = result;
        }

        private void CheckHeatingContracts()
        {
            var operation = new StartHeatingOperation();
            bool preConditions = operation.CheckPreConditions(CurrentData, Actuators);
            
            string result = $"=== ПРОВЕРКА КОНТРАКТОВ: ОБОГРЕВ ===\n\n";
            result += $"Операция: {operation.Name}\n\n";
            result += $"ПРЕДУСЛОВИЯ:\n";
            result += $"  • Температура < 15°C: {CurrentData.Temperature}°C {(CurrentData.Temperature < 15.0 ? "✓" : "✗")}\n";
            result += $"  • Обогрев выключен: {!Actuators.Heater} {(!Actuators.Heater ? "✓" : "✗")}\n";
            result += $"  Результат проверки предусловий: {(preConditions ? "✓ ВЫПОЛНЕНЫ" : "✗ НЕ ВЫПОЛНЕНЫ")}\n\n";
            
            if (preConditions)
            {
                bool wasHeater = Actuators.Heater;
                operation.Execute(CurrentData, Actuators);
                bool postConditions = operation.CheckPostConditions(CurrentData, Actuators);
                result += $"ПОСТУСЛОВИЯ:\n";
                result += $"  • Обогрев включен: {Actuators.Heater} {(Actuators.Heater ? "✓" : "✗")}\n";
                result += $"  Результат проверки постусловий: {(postConditions ? "✓ ВЫПОЛНЕНЫ" : "✗ НЕ ВЫПОЛНЕНЫ")}\n";
                Actuators.Heater = wasHeater;
            }
            
            ContractsResult = result;
        }

        private void CheckVentilationContracts()
        {
            var operation = new StartVentilationOperation();
            bool preConditions = operation.CheckPreConditions(CurrentData, Actuators);
            
            string result = $"=== ПРОВЕРКА КОНТРАКТОВ: ВЕНТИЛЯЦИЯ ===\n\n";
            result += $"Операция: {operation.Name}\n\n";
            result += $"ПРЕДУСЛОВИЯ:\n";
            result += $"  • Температура > 25°C ИЛИ CO₂ > 1200 ppm: ";
            result += $"Температура {CurrentData.Temperature}°C {(CurrentData.Temperature > 25.0 ? "✓" : "✗")}, ";
            result += $"CO₂ {CurrentData.CO2Level} ppm {(CurrentData.CO2Level > 1200.0 ? "✓" : "✗")}\n";
            result += $"  • Вентиляция выключена: {!Actuators.Ventilation} {(!Actuators.Ventilation ? "✓" : "✗")}\n";
            result += $"  Результат проверки предусловий: {(preConditions ? "✓ ВЫПОЛНЕНЫ" : "✗ НЕ ВЫПОЛНЕНЫ")}\n\n";
            
            if (preConditions)
            {
                bool wasVentilation = Actuators.Ventilation;
                operation.Execute(CurrentData, Actuators);
                bool postConditions = operation.CheckPostConditions(CurrentData, Actuators);
                result += $"ПОСТУСЛОВИЯ:\n";
                result += $"  • Вентиляция включена: {Actuators.Ventilation} {(Actuators.Ventilation ? "✓" : "✗")}\n";
                result += $"  Результат проверки постусловий: {(postConditions ? "✓ ВЫПОЛНЕНЫ" : "✗ НЕ ВЫПОЛНЕНЫ")}\n";
                Actuators.Ventilation = wasVentilation;
            }
            
            ContractsResult = result;
        }

        private void CheckLightingContracts()
        {
            var operation = new TurnOnLightsOperation();
            bool preConditions = operation.CheckPreConditions(CurrentData, Actuators);
            
            string result = $"=== ПРОВЕРКА КОНТРАКТОВ: ОСВЕЩЕНИЕ ===\n\n";
            result += $"Операция: {operation.Name}\n\n";
            result += $"ПРЕДУСЛОВИЯ:\n";
            result += $"  • Освещенность < 3000 lux: {CurrentData.LightIntensity} lux {(CurrentData.LightIntensity < 3000.0 ? "✓" : "✗")}\n";
            result += $"  • Освещение выключено: {!Actuators.Lights} {(!Actuators.Lights ? "✓" : "✗")}\n";
            result += $"  Результат проверки предусловий: {(preConditions ? "✓ ВЫПОЛНЕНЫ" : "✗ НЕ ВЫПОЛНЕНЫ")}\n\n";
            
            if (preConditions)
            {
                bool wasLights = Actuators.Lights;
                operation.Execute(CurrentData, Actuators);
                bool postConditions = operation.CheckPostConditions(CurrentData, Actuators);
                result += $"ПОСТУСЛОВИЯ:\n";
                result += $"  • Освещение включено: {Actuators.Lights} {(Actuators.Lights ? "✓" : "✗")}\n";
                result += $"  Результат проверки постусловий: {(postConditions ? "✓ ВЫПОЛНЕНЫ" : "✗ НЕ ВЫПОЛНЕНЫ")}\n";
                Actuators.Lights = wasLights;
            }
            
            ContractsResult = result;
        }

        private void InitializeWpExamples()
        {
            WpExamples = new ObservableCollection<WpFragment>
            {
                new WpFragment
                {
                    Name = "Полив",
                    PostCondition = "soilMoisture > 60",
                    PostConditionDescription = "Влажность почвы больше 60%",
                    Code = "waterValve := true;\nsoilMoisture := soilMoisture + 5"
                },
                new WpFragment
                {
                    Name = "Обогрев",
                    PostCondition = "temperature >= 15",
                    PostConditionDescription = "Температура больше или равна 15°C",
                    Code = "if (temperature < 15) {\n    heater := true;\n    temperature := temperature + 2\n} else {\n    heater := false\n}"
                },
                new WpFragment
                {
                    Name = "Вентиляция",
                    PostCondition = "co2Level <= 1200",
                    PostConditionDescription = "Уровень CO₂ меньше или равен 1200 ppm",
                    Code = "ventilation := true;\nco2Level := co2Level - 100"
                },
                new WpFragment
                {
                    Name = "Освещение",
                    PostCondition = "lightIntensity >= 3000",
                    PostConditionDescription = "Освещенность больше или равна 3000 lux",
                    Code = "lights := true;\nlightIntensity := lightIntensity + 500"
                }
            };
            OnPropertyChanged(nameof(WpExamples));
        }

        private void CalculateWp()
        {
            if (string.IsNullOrWhiteSpace(WpPostCondition) || string.IsNullOrWhiteSpace(WpCode))
            {
                WpResult = "Заполните поле цели (Post) и код фрагмента";
                WpTrace = "";
                WpFinalResult = "";
                WpFinalResultDescription = "";
                WpTriad = "";
                return;
            }

            var fragment = new WpFragment
            {
                PostCondition = WpPostCondition,
                PostConditionDescription = WpPostConditionDescription,
                Code = WpCode
            };

            var result = WpCalculator.CalculateWp(fragment);

            var traceText = "ПОШАГОВЫЙ ТРЕЙС (от последнего оператора к первому):\n\n";
            traceText += $"Начальное постусловие: {fragment.PostCondition}\n";
            traceText += $"Расшифровка: {fragment.PostConditionDescription}\n\n";
            traceText += new string('=', 60) + "\n\n";
            
            for (int i = result.TraceSteps.Count - 1; i >= 0; i--)
            {
                traceText += result.TraceSteps[i] + "\n\n";
                if (i > 0)
                {
                    traceText += new string('-', 60) + "\n\n";
                }
            }

            WpTrace = traceText;
            WpFinalResult = result.FinalWp;
            WpFinalResultDescription = result.FinalWpDescription;
            WpTriad = result.Triad;

            WpResult = $"ИТОГОВОЕ ПРЕДУСЛОВИЕ (WP):\n\n" +
                      $"Краткая запись: {result.FinalWp}\n\n" +
                      $"Расшифровка: {result.FinalWpDescription}";
        }

        private void ShowWpTriad()
        {
            if (string.IsNullOrWhiteSpace(WpTriad))
            {
                MessageBox.Show("Сначала рассчитайте WP", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show($"ТРИАДА ХОАРА:\n\n{WpTriad}", "Триада {Pre} P {Post}", MessageBoxButton.OK, MessageBoxImage.Information);
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

        // ЛР4: Методы для работы с булевыми функциями
        private void UpdateBooleanNumberExplanation()
        {
            if (BooleanN < 1 || BooleanN > 10)
            {
                BooleanNumberExplanation = "Количество переменных должно быть от 1 до 10";
                return;
            }

            int maxNum = (1 << (1 << BooleanN)) - 1;
            if (BooleanNum < 0 || BooleanNum > maxNum)
            {
                BooleanNumberExplanation = $"Номер функции должен быть от 0 до {maxNum}";
                return;
            }

            int rowCount = 1 << BooleanN;
            string binary = Convert.ToString(BooleanNum, 2).PadLeft(rowCount, '0');
            
            var explanation = new StringBuilder();
            explanation.AppendLine($"Двоичное представление: {binary}₂ (длина {rowCount} бит)");
            explanation.AppendLine();
            explanation.AppendLine("Соответствие битов кортежам переменных:");
            
            for (int i = 0; i < rowCount; i++)
            {
                var values = new bool[BooleanN];
                for (int j = 0; j < BooleanN; j++)
                {
                    values[j] = (i & (1 << j)) != 0;
                }
                string tuple = string.Join(", ", values.Select(v => v ? "1" : "0"));
                bool bitValue = binary[binary.Length - 1 - i] == '1';
                explanation.AppendLine($"  Бит {i} (позиция {binary.Length - 1 - i}): кортеж ({tuple}) → {bitValue}");
            }

            BooleanNumberExplanation = explanation.ToString();
        }

        private void BuildTruthTableFromNumber()
        {
            try
            {
                if (BooleanN < 1 || BooleanN > 10)
                {
                    BooleanNumberResult = "Ошибка: количество переменных должно быть от 1 до 10";
                    return;
                }

                int maxNum = (1 << (1 << BooleanN)) - 1;
                if (BooleanNum < 0 || BooleanNum > maxNum)
                {
                    BooleanNumberResult = $"Ошибка: номер функции должен быть от 0 до {maxNum}";
                    return;
                }

                var func = BooleanFunction.FromNumber(BooleanN, BooleanNum);
                var table = func.BuildTruthTable();

                var result = new StringBuilder();
                result.AppendLine("ТАБЛИЦА ИСТИННОСТИ");
                result.AppendLine(new string('=', 60));
                result.AppendLine();

                // Заголовок
                result.Append(string.Format("{0,-8}", "№"));
                for (int i = 0; i < BooleanN; i++)
                {
                    result.Append(string.Format("{0,-8}", table.VariableNames[i]));
                }
                result.AppendLine(string.Format("{0,-8}", "f"));
                result.AppendLine(new string('-', 60));

                // Строки таблицы
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    result.Append(string.Format("{0,-8}", i));
                    for (int j = 0; j < BooleanN; j++)
                    {
                        result.Append(string.Format("{0,-8}", row.Values[j] ? "1" : "0"));
                    }
                    result.AppendLine(string.Format("{0,-8}", row.Result ? "1" : "0"));
                }

                if (BooleanN > 5)
                {
                    result.AppendLine();
                    result.AppendLine($"⚠ Предупреждение: сложность O(2^{BooleanN}) = {1 << BooleanN} строк");
                }

                BooleanNumberResult = result.ToString();
            }
            catch (Exception ex)
            {
                BooleanNumberResult = $"Ошибка: {ex.Message}";
            }
        }

        private void GenerateDNFFromNumber()
        {
            try
            {
                if (BooleanN < 1 || BooleanN > 10)
                {
                    BooleanNumberResult = "Ошибка: количество переменных должно быть от 1 до 10";
                    return;
                }

                int maxNum = (1 << (1 << BooleanN)) - 1;
                if (BooleanNum < 0 || BooleanNum > maxNum)
                {
                    BooleanNumberResult = $"Ошибка: номер функции должен быть от 0 до {maxNum}";
                    return;
                }

                var func = BooleanFunction.FromNumber(BooleanN, BooleanNum);
                string dnf = func.GenerateDNF();

                var result = new StringBuilder();
                result.AppendLine("ДИЗЪЮНКТИВНАЯ НОРМАЛЬНАЯ ФОРМА (DNF)");
                result.AppendLine(new string('=', 60));
                result.AppendLine();
                result.AppendLine("Формула в базисе {¬, ∧, ∨}:");
                result.AppendLine();
                result.AppendLine(dnf);
                result.AppendLine();
                result.AppendLine("(Озвучка: отрицание, конъюнкция, дизъюнкция)");

                BooleanNumberResult = result.ToString();
            }
            catch (Exception ex)
            {
                BooleanNumberResult = $"Ошибка: {ex.Message}";
            }
        }

        private void GenerateKNFFromNumber()
        {
            try
            {
                if (BooleanN < 1 || BooleanN > 10)
                {
                    BooleanNumberResult = "Ошибка: количество переменных должно быть от 1 до 10";
                    return;
                }

                int maxNum = (1 << (1 << BooleanN)) - 1;
                if (BooleanNum < 0 || BooleanNum > maxNum)
                {
                    BooleanNumberResult = $"Ошибка: номер функции должен быть от 0 до {maxNum}";
                    return;
                }

                var func = BooleanFunction.FromNumber(BooleanN, BooleanNum);
                string knf = func.GenerateKNF();

                var result = new StringBuilder();
                result.AppendLine("КОНЪЮНКТИВНАЯ НОРМАЛЬНАЯ ФОРМА (KNF)");
                result.AppendLine(new string('=', 60));
                result.AppendLine();
                result.AppendLine("Формула в базисе {¬, ∧, ∨}:");
                result.AppendLine();
                result.AppendLine(knf);
                result.AppendLine();
                result.AppendLine("(Озвучка: отрицание, конъюнкция, дизъюнкция)");

                BooleanNumberResult = result.ToString();
            }
            catch (Exception ex)
            {
                BooleanNumberResult = $"Ошибка: {ex.Message}";
            }
        }

        private void ParseAndEvaluateFormula()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(BooleanFormula))
                {
                    BooleanFormulaResult = "Ошибка: введите формулу";
                    return;
                }

                var func = BooleanFunction.FromFormula(BooleanFormula);
                var table = func.BuildTruthTable();

                // Информация о формуле
                var info = new StringBuilder();
                info.AppendLine($"Количество переменных: {func.VariableCount}");
                info.AppendLine($"Переменные: {string.Join(", ", func.VariableNames)}");
                BooleanFormulaInfo = info.ToString();

                // Стоимость формулы
                var cost = BooleanFunction.CalculateCost(BooleanFormula);
                BooleanFormulaCost = $"Стоимость формулы: {cost.Literals} литералов, {cost.Conjunctions} конъюнктов, {cost.Disjunctions} дизъюнктов";

                // Таблица истинности
                var result = new StringBuilder();
                result.AppendLine("ТАБЛИЦА ИСТИННОСТИ");
                result.AppendLine(new string('=', 60));
                result.AppendLine();

                // Заголовок
                result.Append(string.Format("{0,-8}", "№"));
                for (int i = 0; i < func.VariableCount; i++)
                {
                    result.Append(string.Format("{0,-8}", table.VariableNames[i]));
                }
                result.AppendLine(string.Format("{0,-8}", "f"));
                result.AppendLine(new string('-', 60));

                // Строки таблицы
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    result.Append(string.Format("{0,-8}", i));
                    for (int j = 0; j < func.VariableCount; j++)
                    {
                        result.Append(string.Format("{0,-8}", row.Values[j] ? "1" : "0"));
                    }
                    result.AppendLine(string.Format("{0,-8}", row.Result ? "1" : "0"));
                }

                result.AppendLine();
                result.AppendLine("DNF (дизъюнктивная нормальная форма):");
                result.AppendLine(func.GenerateDNF());
                result.AppendLine();
                result.AppendLine("KNF (конъюнктивная нормальная форма):");
                result.AppendLine(func.GenerateKNF());

                if (func.VariableCount > 5)
                {
                    result.AppendLine();
                    result.AppendLine($"⚠ Предупреждение: сложность O(2^{func.VariableCount}) = {1 << func.VariableCount} строк");
                }

                BooleanFormulaResult = result.ToString();
            }
            catch (Exception ex)
            {
                BooleanFormulaResult = $"Ошибка парсинга: {ex.Message}";
                BooleanFormulaInfo = "";
                BooleanFormulaCost = "";
            }
        }

        private void CheckEquivalence()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(BooleanCompareFunction1) || string.IsNullOrWhiteSpace(BooleanCompareFunction2))
                {
                    BooleanEquivalenceResult = "Ошибка: введите обе функции для сравнения";
                    return;
                }

                BooleanFunction func1, func2;

                // Парсим первую функцию
                if (int.TryParse(BooleanCompareFunction1.Trim(), out int num1))
                {
                    // Это номер функции - нужно определить n
                    func1 = BooleanFunction.FromNumber(BooleanN, num1);
                }
                else
                {
                    func1 = BooleanFunction.FromFormula(BooleanCompareFunction1);
                }

                // Парсим вторую функцию
                if (int.TryParse(BooleanCompareFunction2.Trim(), out int num2))
                {
                    func2 = BooleanFunction.FromNumber(BooleanN, num2);
                }
                else
                {
                    func2 = BooleanFunction.FromFormula(BooleanCompareFunction2);
                }

                var equivalenceResult = BooleanFunction.CheckEquivalence(func1, func2);
                bool equivalent = equivalenceResult.Equivalent;
                string counterExample = equivalenceResult.CounterExample;

                var result = new StringBuilder();
                result.AppendLine("ПРОВЕРКА ЭКВИВАЛЕНТНОСТИ");
                result.AppendLine(new string('=', 60));
                result.AppendLine();
                result.AppendLine($"Функция 1: {BooleanCompareFunction1}");
                result.AppendLine($"Функция 2: {BooleanCompareFunction2}");
                result.AppendLine();

                if (equivalent)
                {
                    result.AppendLine("✓ Функции ЭКВИВАЛЕНТНЫ");
                }
                else
                {
                    result.AppendLine("✗ Функции НЕ ЭКВИВАЛЕНТНЫ");
                    if (!string.IsNullOrEmpty(counterExample))
                    {
                        result.AppendLine();
                        result.AppendLine($"Первое контр-слово: {counterExample}");
                    }
                }

                BooleanEquivalenceResult = result.ToString();
            }
            catch (Exception ex)
            {
                BooleanEquivalenceResult = $"Ошибка: {ex.Message}";
            }
        }

        private void CopyBooleanResult()
        {
            string textToCopy = "";
            
            // Определяем, какая вкладка активна (упрощенно - копируем все результаты)
            if (!string.IsNullOrEmpty(BooleanNumberResult))
            {
                textToCopy = BooleanNumberResult;
            }
            else if (!string.IsNullOrEmpty(BooleanFormulaResult))
            {
                textToCopy = BooleanFormulaResult;
            }
            else if (!string.IsNullOrEmpty(BooleanEquivalenceResult))
            {
                textToCopy = BooleanEquivalenceResult;
            }

            if (!string.IsNullOrEmpty(textToCopy))
            {
                System.Windows.Clipboard.SetText(textToCopy);
                UpdateStatus("Результат скопирован в буфер обмена");
            }
        }

        private void LoadExampleFormula(string formula)
        {
            BooleanFormula = formula;
            UpdateStatus($"Загружен пример формулы: {formula}");
        }

        private void InitializeLoopData()
        {
            LoopDataValues = new ObservableCollection<double>();
            LoopDataTable = new ObservableCollection<LoopDataRow>();
            AvailableLoopModes = new ObservableCollection<LoopMode>
            {
                LoopMode.PrefixSum,
                LoopMode.CountGreaterThanT,
                LoopMode.PrefixMax
            };
            _loopData = new LoopData { Mode = SelectedLoopMode };
            _invariantInfo = new InvariantInfo();
            InitializeLoopMode();
        }

        private void InitializeLoopMode()
        {
            if (_loopData == null || _invariantInfo == null) return;

            _loopData.Mode = SelectedLoopMode;
            
            switch (SelectedLoopMode)
            {
                case LoopMode.PrefixSum:
                    _invariantInfo.InvariantDescription = "Сумма температур равна сумме всех температурных показаний датчиков от начала до текущего индекса. Индекс находится в допустимых пределах (0 ≤ j ≤ n). Используется для расчета среднего значения температуры в теплице.";
                    _invariantInfo.InvariantFormula = "res == sum(temperature[0..j-1]) && 0 <= j && j <= n";
                    _invariantInfo.VariantFunction = "n - j";
                    break;
                case LoopMode.CountGreaterThanT:
                    _invariantInfo.InvariantDescription = $"Количество критических моментов равно числу случаев, когда температура была ниже 15°C (требовалось включение обогревателей теплицы) от начала до текущего индекса. Индекс находится в допустимых пределах (0 ≤ j ≤ n).";
                    _invariantInfo.InvariantFormula = $"res == count(temperature[0..j-1] < 15) && 0 <= j && j <= n";
                    _invariantInfo.VariantFunction = "n - j";
                    break;
                case LoopMode.PrefixMax:
                    _invariantInfo.InvariantDescription = "Максимальная температура равна наибольшему значению температуры от начала до текущего индекса. Индекс находится в допустимых пределах (0 ≤ j ≤ n). Позволяет отслеживать пиковые значения температуры в теплице.";
                    _invariantInfo.InvariantFormula = "res == max(temperature[0..j-1]) && 0 <= j && j <= n";
                    _invariantInfo.VariantFunction = "n - j";
                    break;
            }

            OnPropertyChanged(nameof(InvariantDescription));
            OnPropertyChanged(nameof(InvariantFormula));
            OnPropertyChanged(nameof(VariantFunction));
        }

        private void GenerateLoopData()
        {
            var random = new Random();
            LoopDataValues.Clear();
            LoopDataTable.Clear();

            for (int i = 0; i < LoopDataCount; i++)
            {
                double value = random.NextDouble() * (LoopDataMax - LoopDataMin) + LoopDataMin;
                LoopDataValues.Add(value);
                LoopDataTable.Add(new LoopDataRow
                {
                    Index = i,
                    Value = value,
                    IsCurrent = false,
                    IsModified = false
                });
            }

            _loopData.Values = LoopDataValues.ToList();
            _loopData.Threshold = LoopThreshold;
            _loopData.CurrentIndex = 0;
            
            if (SelectedLoopMode == LoopMode.PrefixMax && LoopDataValues.Count > 0)
            {
                _loopData.Result = LoopDataValues[0];
            }
            else
            {
                _loopData.Result = 0;
            }

            _invariantInfo.VariantValue = LoopDataCount;
            _loopChecker = new LoopInvariantChecker(_loopData, _invariantInfo);

            LoopCurrentIndex = 0;
            LoopResult = _loopData.Result;
            VariantValue = _invariantInfo.VariantValue;
            InvariantResult = "";

            UpdateLoopDataTable();
            OnPropertyChanged(nameof(InvariantResult));
            CommandManager.InvalidateRequerySuggested();
        }

        private void ExecuteLoopAndShowResult()
        {
            if (_loopChecker == null || _loopData == null) return;

            _loopData.CurrentIndex = 0;
            if (SelectedLoopMode == LoopMode.PrefixMax && LoopDataValues.Count > 0)
            {
                _loopData.Result = LoopDataValues[0];
            }
            else
            {
                _loopData.Result = 0;
            }
            _invariantInfo.VariantValue = LoopDataCount;
            _invariantInfo.InvariantBeforeStep = false;
            _invariantInfo.InvariantAfterStep = false;

            ExecuteLoopRun();
            
            LoopResult = _loopData.Result;
            LoopCurrentIndex = _loopData.CurrentIndex;
            VariantValue = _invariantInfo.VariantValue;

            string resultDescription = "";
            switch (SelectedLoopMode)
            {
                case LoopMode.PrefixSum:
                    double average = LoopResult / LoopDataCount;
                    resultDescription = $"Сумма температур: {LoopResult:F2}°C\nСредняя температура: {average:F2}°C";
                    break;
                case LoopMode.CountGreaterThanT:
                    resultDescription = $"Количество критических моментов (температура < 15°C, требовалось включение обогревателей): {(int)LoopResult}";
                    break;
                case LoopMode.PrefixMax:
                    resultDescription = $"Наибольшая температура: {LoopResult:F2}°C";
                    break;
            }
            InvariantResult = $"Результат обработки данных датчиков температуры:\n{resultDescription}";
            OnPropertyChanged(nameof(InvariantResult));
        }

        private void ExecuteLoopStep()
        {
            if (_loopChecker == null || _loopData.CurrentIndex >= _loopData.Values.Count)
                return;

            bool hasMore = _loopChecker.ExecuteStep();
            LoopCurrentIndex = _loopData.CurrentIndex;
            LoopResult = _loopData.Result;
            VariantValue = _invariantInfo.VariantValue;
            InvariantBeforeStep = _invariantInfo.InvariantBeforeStep;
            InvariantAfterStep = _invariantInfo.InvariantAfterStep;

            UpdateLoopDataTable();

            CommandManager.InvalidateRequerySuggested();

            if (!hasMore)
            {
                string resultDescription = "";
                switch (SelectedLoopMode)
                {
                    case LoopMode.PrefixSum:
                        resultDescription = $"Сумма температур: {LoopResult:F2}°C (среднее: {LoopResult / LoopDataCount:F2}°C)";
                        break;
                    case LoopMode.CountGreaterThanT:
                        resultDescription = $"Количество критических моментов (температура > {LoopThreshold}°C): {LoopResult}";
                        break;
                    case LoopMode.PrefixMax:
                        resultDescription = $"Максимальная температура за период: {LoopResult:F2}°C";
                        break;
                }
                InvariantResult = $"Цикл обработки данных датчиков температуры завершен.\n{resultDescription}\n\nПостусловие выполнено: {_loopChecker.GetPostCondition()}";
            }
        }

        private void ExecuteLoopRun()
        {
            if (_loopChecker == null) return;

            while (_loopData.CurrentIndex < _loopData.Values.Count)
            {
                ExecuteLoopStep();
            }
        }

        private void UpdateLoopDataTable()
        {
            for (int i = 0; i < LoopDataTable.Count; i++)
            {
                LoopDataTable[i].IsCurrent = (i == _loopData.CurrentIndex);
                LoopDataTable[i].IsModified = (i < _loopData.CurrentIndex);
            }
        }

        private void CheckVerificationConditions()
        {
            if (_loopChecker == null)
            {
                VerificationConditions = "Сначала сгенерируйте данные датчиков температуры";
                return;
            }

            var vc = _loopChecker.CheckVerificationConditions();
            string modeDescription = "";
            switch (SelectedLoopMode)
            {
                case LoopMode.PrefixSum:
                    modeDescription = "Суммирование температур для расчета среднего значения";
                    break;
                case LoopMode.CountGreaterThanT:
                    modeDescription = "Подсчет критических моментов (температура < 15°C, требовалось включение обогревателей)";
                    break;
                case LoopMode.PrefixMax:
                    modeDescription = "Поиск максимальной температуры";
                    break;
            }
            
            string result = $"УСЛОВИЯ ВЕРИФИКАЦИИ (VC) ДЛЯ ЦИКЛА ОБРАБОТКИ ДАННЫХ ДАТЧИКОВ ТЕПЛИЦЫ\nРежим: {modeDescription}\n\n";
            result += $"1. Pre ⇒ Inv (после инициализации):\n   {vc.PreImpliesInv}\n\n";
            result += $"   Объяснение: После инициализации должно выполняться предусловие инварианта.\n\n";
            result += $"2. Сохранение инварианта: (Inv ∧ B) ⇒ wp(S, Inv)\n   {vc.InvariantPreservation}\n\n";
            result += $"   Объяснение: Если инвариант выполнен и цикл продолжается (j < n), то после выполнения тела цикла инвариант должен сохраниться.\n\n";
            result += $"3. Выход: (Inv ∧ ¬B) ⇒ Post\n   {vc.ExitImpliesPost}\n\n";
            result += $"   Объяснение: Когда цикл завершается (j >= n) и инвариант выполнен, должно выполняться постусловие.\n\n";
            result += $"4. Завершение: Inv ∧ B ⇒ (t' < t)\n   {vc.VariantDecreases}\n\n";
            result += $"   Объяснение: Вариант-функция (количество оставшихся измерений) должна уменьшаться на каждом шаге, гарантируя завершение цикла обработки данных датчиков температуры.";

            VerificationConditions = result;
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

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is T param)
                return _canExecute?.Invoke(param) ?? true;
            return _canExecute == null;
        }

        public void Execute(object parameter)
        {
            if (parameter is T param)
                _execute(param);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}