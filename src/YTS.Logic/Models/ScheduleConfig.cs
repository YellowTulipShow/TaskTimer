using Cronos;

namespace YTS.Logic.Models
{
    public sealed class ScheduleConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "新任务";
        public string FilePath { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";

        /// <summary>5 段标准 Cron：分 时 日 月 周</summary>
        public string Cron { get; set; } = "0 9 * * *";

        public bool Enabled { get; set; } = true;

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTimeOffset? NextRunAt { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public int? RunningProcessId { get; set; }

        public DateTimeOffset? ComputeNext(DateTimeOffset fromUtc)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(Cron)) return null;
            try
            {
                var expr = CronExpression.Parse(Cron, CronFormat.Standard);
                return expr.GetNextOccurrence(fromUtc, TimeZoneInfo.Local);
            }
            catch { return null; }
        }

        public ScheduleConfig Clone() => (ScheduleConfig)MemberwiseClone();
    }
}
