namespace YTS.Logic.Models
{
    /// <summary>
    /// 应用程序
    /// </summary>
    public sealed class AppConfig
    {
        public IList<ScheduleConfig> Schedules { get; set; } = [];
        public bool CloseToTray { get; set; } = true;
    }
}
