namespace YTS.Logic.Models
{
    public sealed class AppConfig
    {
        public IList<ScheduleConfig> Schedules { get; set; } = [];
        public bool AutoStartWithWindows { get; set; }
        public bool StartMinimizedToTray { get; set; }
        public bool CloseToTray { get; set; } = true;
    }
}
