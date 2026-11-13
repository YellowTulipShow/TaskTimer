namespace YTS.Logic.Models
{
    /// <summary>
    /// 应用程序
    /// </summary>
    public sealed class AppConfig
    {
        /// <summary>
        /// 日程配置
        /// </summary>
        public IList<ScheduleConfig> Schedules { get; set; } = [];
        /// <summary>
        /// 是否关闭窗口变为最小化
        /// </summary>
        public bool CloseToTray { get; set; } = true;
        /// <summary>
        /// 日志展示最大行数
        /// </summary>
        public int LogMaxLineCount { get; set; } = 1000;
    }
}
