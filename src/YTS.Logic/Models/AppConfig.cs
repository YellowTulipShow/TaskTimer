using YTS.Logic.Editors;

namespace YTS.Logic.Models
{
    /// <summary>
    /// 数据模型: 引用程序配置
    /// </summary>
    public sealed class AppConfig
    {
        /// <summary>
        /// 日程配置
        /// </summary>
        public IList<ScheduleConfig> Schedules { get; set; } = [];

        /// <summary>
        /// 系统配置
        /// </summary>
        public AppConfigSystem System { get; set; } = new AppConfigSystem()
        {
            CloseToTray = true,
        };

        /// <summary>
        /// 日志控件配置
        /// </summary>
        public AppConfigLogControl LogConfig { get; set; } = new AppConfigLogControl()
        {
            LogMaxLineCount = 1000,
            IsOuputDebugLog = true,
        };
    }

    /// <summary>
    /// 数据模型: 引用程序配置日志控件
    /// </summary>
    public sealed class AppConfigSystem
    {
        /// <summary>
        /// 是否关闭窗口变为最小化
        /// </summary>
        [Display(Name = "最小化", Description = "是否关闭窗口变为最小化")]
        public bool CloseToTray { get; set; }
    }

    /// <summary>
    /// 数据模型: 引用程序配置日志控件
    /// </summary>
    public sealed class AppConfigLogControl
    {
        /// <summary>
        /// 日志展示最大行数
        /// </summary>
        [Display(Name = "最大行数", Description = "日志展示控件可展示的最大行数")]
        public int LogMaxLineCount { get; set; }

        /// <summary>
        /// 是否输出调试日志内容
        /// </summary>
        [Display(Name = "调试日志", Description = "是否输出调试日志内容")]
        public bool IsOuputDebugLog { get; set; }
    }
}
