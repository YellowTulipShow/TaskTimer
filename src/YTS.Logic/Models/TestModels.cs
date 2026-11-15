using YTS.Logic.Editors;

namespace YTS.TaskTimer.UITools.Editors.TestModels;

// ═════════════════════════════════════════════════════════
//  模型 1：简单模型 —— 少量字段，单列能放下
//  · 3 个字段，其中 2 个带描述
//  · 用于验证：基本布局、Name 列对齐、Description 显示
// ═════════════════════════════════════════════════════════

public class SimpleTestModel
{
    [Display(Name = "用户名", Description = "登录系统使用的账号名称")]
    public string UserName { get; set; } = "admin";

    [Display(Name = "启用")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "重试次数", Description = "任务失败后的最大重试次数")]
    public int RetryCount { get; set; } = 3;
}

// ═════════════════════════════════════════════════════════
//  模型 2：完整模型 —— 覆盖所有编辑器类型，触发双列
//  · 16 个字段，其中 13 个带描述、3 个无描述
//  · 覆盖：string / multiline / options(单选/多选) / bool
//         / int / long / double / decimal
//         / DateTime(默认/自定义格式) / enum
//  · 单列高度 ≈ 789px > 75% 屏高阈值，自动切换双列
// ═════════════════════════════════════════════════════════

public enum TaskPriority { Low = 0, Normal = 1, High = 2, Critical = 3 }
public enum TaskStatus { Pending, Running, Completed, Failed, Cancelled }

public class FullTestModel
{
    // ── 字符串：普通 ─────────────────────────────
    [Display(Name = "任务名称", Description = "任务的展示名称，建议简短明了")]
    public string Name { get; set; } = "示例任务";

    [Display(Name = "脚本路径", Description = "要执行的程序或脚本的完整路径")]
    public string ScriptPath { get; set; } = @"C:\Windows\System32\notepad.exe";

    // ── 字符串：无描述（验证无描述行时不占位）──
    [Display(Name = "工作目录")]
    public string WorkingDirectory { get; set; } = "";

    // ── 字符串：多行 ────────────────────────────
    [Display(Name = "启动参数", Description = "传递给脚本的命令行参数")]
    [Multiline(2)]
    public string Arguments { get; set; } = "";

    [Display(Name = "备注说明", Description = "对内备注，不会展示给最终用户，可写多行")]
    [Multiline(5)]
    public string Remarks { get; set; } = "";

    // ── 字符串：下拉单选 ────────────────────────
    [Display(Name = "触发类型", Description = "任务的触发方式")]
    [Options("定时触发", "间隔触发", "事件触发", "手动触发")]
    public string TriggerType { get; set; } = "定时触发";

    // ── 字符串：多选（自定义分隔符）────────────
    [Display(Name = "标签", Description = "任务所属标签，可多选")]
    [Options("日常", "紧急", "系统", "备份", "监控" , MultiSelect = true, Separator = "|")]
    public string Tags { get; set; } = "日常|监控";

    // ── 枚举 ────────────────────────────────────
    [Display(Name = "优先级", Description = "任务执行优先级，越高越先调度")]
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    [Display(Name = "初始状态")]
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    // ── 布尔 ────────────────────────────────────
    [Display(Name = "启用", Description = "勾选后任务会参与调度")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "失败时发送通知")]
    public bool NotifyOnFailure { get; set; } = false;

    // ── 整数 ────────────────────────────────────
    [Display(Name = "重试次数", Description = "执行失败后自动重试的最大次数，0 表示不重试")]
    public int RetryCount { get; set; } = 3;

    [Display(Name = "超时（秒）", Description = "单次执行的超时时间，超时后强制终止")]
    public long TimeoutSeconds { get; set; } = 300;

    // ── 浮点数 ──────────────────────────────────
    [Display(Name = "权重", Description = "并发调度时的相对权重，用于负载均衡")]
    public double Weight { get; set; } = 1.0;

    [Display(Name = "成功率阈值", Description = "低于该成功率触发告警，范围 0~1")]
    public decimal SuccessRateThreshold { get; set; } = 0.95m;

    // ── 日期时间 ────────────────────────────────
    [Display(Name = "开始时间", Description = "任务首次执行的计划时间")]
    public DateTime StartTime { get; set; } = DateTime.Now;

    [Display(Name = "结束时间", Description = "任务不再触发的截止时间，格式为 yyyy-MM-dd HH:mm")]
    [Format("yyyy-MM-dd HH:mm")]
    public DateTime EndTime { get; set; } = DateTime.Now.AddDays(30);

    [Display(Name = "最后执行")]
    [Format("yyyy-MM-dd HH:mm:ss.fff")]
    public DateTime LastRunAt { get; set; } = DateTime.MinValue.AddYears(2025);
}

// ═════════════════════════════════════════════════════════
//  模型 3：超大模型 —— 30 个字段，双列也装不下，触发滚动
//  · 覆盖所有编辑器类型，重复使用以填满数量
//  · 双列后每列高度 ≈ 810px > 75% 屏高，必须滚动
//  · 用于验证：滚动行为、75% 屏高上限、双列划分
// ═════════════════════════════════════════════════════════

public class LargeTestModel
{
    // ── 基础标识（5 个）────────────────────────
    [Display(Name = "任务 ID", Description = "全局唯一标识，由系统自动生成")]
    public string TaskId { get; set; } = Guid.NewGuid().ToString("N");

    [Display(Name = "任务名称", Description = "任务的展示名称")]
    public string Name { get; set; } = "批量任务";

    [Display(Name = "分类")]
    public string Category { get; set; } = "通用";

    [Display(Name = "负责人", Description = "任务的责任人，用于告警通知")]
    public string Owner { get; set; } = "";

    [Display(Name = "所属项目", Description = "任务归属的项目名称")]
    public string Project { get; set; } = "";

    // ── 执行配置（6 个）────────────────────────
    [Display(Name = "脚本路径", Description = "要执行的程序或脚本路径")]
    public string ScriptPath { get; set; } = "";

    [Display(Name = "工作目录")]
    public string WorkingDirectory { get; set; } = "";

    [Display(Name = "启动参数", Description = "传给脚本的命令行参数")]
    [Multiline(3)]
    public string Arguments { get; set; } = "";

    [Display(Name = "环境变量", Description = "每行一组 KEY=VALUE，运行时注入到子进程")]
    [Multiline(6)]
    public string EnvironmentVars { get; set; } = "";

    [Display(Name = "超时秒数", Description = "超时后强制终止进程树")]
    public long TimeoutSeconds { get; set; } = 600;

    [Display(Name = "失败重试", Description = "自动重试的最大次数")]
    public int RetryCount { get; set; } = 2;

    // ── 触发规则（5 个）────────────────────────
    [Display(Name = "触发类型", Description = "选择任务的触发方式")]
    [Options("Cron 表达式", "固定间隔", "文件变更", "手动触发")]
    public string TriggerType { get; set; } = "Cron 表达式";

    [Display(Name = "Cron 表达式", Description = "5 段标准 Cron，例：0 9 * * 1-5")]
    public string CronExpression { get; set; } = "0 9 * * 1-5";

    [Display(Name = "开始时间", Description = "早于该时间不会触发")]
    [Format("yyyy-MM-dd HH:mm")]
    public DateTime StartTime { get; set; } = DateTime.Now;

    [Display(Name = "结束时间", Description = "晚于该时间自动停用")]
    [Format("yyyy-MM-dd HH:mm")]
    public DateTime EndTime { get; set; } = DateTime.Now.AddMonths(1);

    [Display(Name = "启用", Description = "禁用后不会参与调度")]
    public bool Enabled { get; set; } = true;

    // ── 状态与统计（5 个）──────────────────────
    [Display(Name = "当前状态")]
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    [Display(Name = "优先级", Description = "数值越高越优先调度")]
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    [Display(Name = "最后执行时间")]
    [Format("yyyy-MM-dd HH:mm:ss")]
    public DateTime LastRunAt { get; set; } = DateTime.Now;

    [Display(Name = "累计执行次数", Description = "任务启动以来成功+失败的总次数")]
    public long TotalRuns { get; set; } = 0;

    [Display(Name = "累计失败次数")]
    public long TotalFailures { get; set; } = 0;

    // ── 阈值与告警（4 个）──────────────────────
    [Display(Name = "成功率阈值", Description = "低于该值触发告警，范围 0~1")]
    public decimal SuccessRateThreshold { get; set; } = 0.9m;

    [Display(Name = "连续失败阈值", Description = "连续失败达到该次数时暂停任务")]
    public int ConsecutiveFailureLimit { get; set; } = 5;

    [Display(Name = "失败时通知")]
    public bool NotifyOnFailure { get; set; } = true;

    [Display(Name = "通知方式", Description = "任务失败时的通知渠道，可多选")]
    [Options("邮件", "短信", "企业微信", "钉钉", "Webhook", MultiSelect = true, Separator = ",")]
    public string NotifyChannels { get; set; } = "邮件";

    // ── 其它（5 个）────────────────────────────
    [Display(Name = "权重", Description = "并发调度的相对权重")]
    public double Weight { get; set; } = 1.0;

    [Display(Name = "并发限制", Description = "同一任务最多同时运行几个实例")]
    public int MaxConcurrency { get; set; } = 1;

    [Display(Name = "标签", Description = "任务分类标签，可多选")]
    [Options("核心", "边缘", "批处理", "实时", "数据", "运维", MultiSelect = true, Separator = "|")]
    public string Tags { get; set; } = "核心";

    [Display(Name = "备注", Description = "对内备注信息，可多行")]
    [Multiline(4)]
    public string Remarks { get; set; } = "";

    [Display(Name = "创建人")]
    public string CreatedBy { get; set; } = "system";
}
