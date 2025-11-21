using System.Windows;

namespace SmartGreenhouse.App
{
    public partial class ContractWindow : Window
    {
        public ContractWindow(OperationViewModel operation)
        {
            InitializeComponent();
            DataContext = new ContractViewModel(operation);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class ContractViewModel
    {
        private readonly OperationViewModel _operation;

        public ContractViewModel(OperationViewModel operation)
        {
            _operation = operation;
        }

        public string OperationName => _operation == null ? "Контракты всех операций" : $"Контракт: {_operation.Name}";

        public string PreConditions
        {
            get
            {
                if (_operation == null)
                {
                    return "ПОЛИВ:\n• Влажность почвы < 60%\n\n" +
                           "ОБОГРЕВ:\n• Температура < 15°C\n\n" +
                           "ВЕНТИЛЯЦИЯ:\n• CO₂ < 1000 ppm\n\n" +
                           "ОСВЕЩЕНИЕ:\n• Освещенность < 3000 lux";
                }

                switch (_operation.Name)
                {
                    case "Полив":
                        return "• Влажность почвы < 60%";
                    case "Обогрев":
                        return "• Температура < 15°C";
                    case "Вентиляция":
                        return "• CO₂ < 1000 ppm";
                    case "Освещение":
                        return "• Освещенность < 3000 lux";
                    default:
                        return "";
                }
            }
        }

        public string PostConditions
        {
            get
            {
                if (_operation == null)
                {
                    return "ПОЛИВ:\n• Клапан полива открыт\n\n" +
                           "ОБОГРЕВ:\n• Обогрев включен\n\n" +
                           "ВЕНТИЛЯЦИЯ:\n• Вентиляция включена\n\n" +
                           "ОСВЕЩЕНИЕ:\n• Освещение включено";
                }

                switch (_operation.Name)
                {
                    case "Полив":
                        return "• Клапан полива открыт";
                    case "Обогрев":
                        return "• Обогрев включен";
                    case "Вентиляция":
                        return "• Вентиляция включена";
                    case "Освещение":
                        return "• Освещение включено";
                    default:
                        return "";
                }
            }
        }

        public string Effects
        {
            get
            {
                if (_operation == null)
                {
                    return "ПОЛИВ: Открывает клапан полива, начинается подача воды в почву.\n\n" +
                           "ОБОГРЕВ: Включает систему обогрева, температура начинает повышаться.\n\n" +
                           "ВЕНТИЛЯЦИЯ: Включает систему вентиляции, снижает температуру и уровень CO₂.\n\n" +
                           "ОСВЕЩЕНИЕ: Включает искусственное освещение, увеличивает уровень освещенности.\n\n" +
                           "ИСКЛЮЧЕНИЯ: InvalidOperationException если предусловия не выполнены.";
                }

                switch (_operation.Name)
                {
                    case "Полив":
                        return "Эффект: Открывает клапан полива, начинается подача воды в почву.\nИсключения: InvalidOperationException если предусловия не выполнены.";
                    case "Обогрев":
                        return "Эффект: Включает систему обогрева, температура начинает повышаться.\nИсключения: InvalidOperationException если предусловия не выполнены.";
                    case "Вентиляция":
                        return "Эффект: Включает систему вентиляции, снижает температуру и уровень CO₂.\nИсключения: InvalidOperationException если предусловия не выполнены.";
                    case "Освещение":
                        return "Эффект: Включает искусственное освещение, увеличивает уровень освещенности.\nИсключения: InvalidOperationException если предусловия не выполнены.";
                    default:
                        return "";
                }
            }
        }

        public string ValidExample
        {
            get
            {
                if (_operation == null)
                {
                    return "ПОЛИВ:\nВлажность почвы: 55%\n→ Операция выполняется успешно\n\n" +
                           "ОБОГРЕВ:\nТемпература: 12°C\n→ Операция выполняется успешно\n\n" +
                           "ВЕНТИЛЯЦИЯ:\nCO₂: 900 ppm\n→ Операция выполняется успешно\n\n" +
                           "ОСВЕЩЕНИЕ:\nОсвещенность: 2500 lux\n→ Операция выполняется успешно";
                }

                switch (_operation.Name)
                {
                    case "Полив":
                        return "Влажность почвы: 55%\n→ Операция выполняется успешно";
                    case "Обогрев":
                        return "Температура: 12°C\n→ Операция выполняется успешно";
                    case "Вентиляция":
                        return "CO₂: 900 ppm\n→ Операция выполняется успешно";
                    case "Освещение":
                        return "Освещенность: 2500 lux\n→ Операция выполняется успешно";
                    default:
                        return "";
                }
            }
        }

        public string InvalidExample
        {
            get
            {
                if (_operation == null)
                {
                    return "ПОЛИВ:\nВлажность почвы: 65% (≥ 60%)\n→ Операция не выполняется\n\n" +
                           "ОБОГРЕВ:\nТемпература: 18°C (≥ 15°C)\n→ Операция не выполняется\n\n" +
                           "ВЕНТИЛЯЦИЯ:\nCO₂: 1100 ppm (≥ 1000 ppm)\n→ Операция не выполняется\n\n" +
                           "ОСВЕЩЕНИЕ:\nОсвещенность: 4000 lux (≥ 3000 lux)\n→ Операция не выполняется";
                }

                switch (_operation.Name)
                {
                    case "Полив":
                        return "Влажность почвы: 65% (≥ 60%)\n→ Операция не выполняется";
                    case "Обогрев":
                        return "Температура: 18°C (≥ 15°C)\n→ Операция не выполняется";
                    case "Вентиляция":
                        return "CO₂: 1100 ppm (≥ 1000 ppm)\n→ Операция не выполняется";
                    case "Освещение":
                        return "Освещенность: 4000 lux (≥ 3000 lux)\n→ Операция не выполняется";
                    default:
                        return "";
                }
            }
        }
    }
}

