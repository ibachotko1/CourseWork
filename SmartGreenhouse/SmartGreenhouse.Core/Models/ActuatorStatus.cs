namespace SmartGreenhouse.Core.Models
{
    /// <summary>
    /// Статус исполнительных механизмов
    /// ЛР4: Булевы состояния для логических функций
    /// </summary>
    public class ActuatorStatus
    {
        public bool WaterValve { get; set; }      // Клапан полива
        public bool Heater { get; set; }          // Обогреватель  
        public bool Ventilation { get; set; }     // Вентиляция
        public bool Lights { get; set; }          // Освещение
        public bool SunProtection { get; set; }   // Защита от солнца
    }
}
