using System.Diagnostics;
using YTS.Logic.Models;

namespace YTS.Logic.Implement
{
    public interface ISchedulerEngine : IDisposable
    {
        event Action<ScheduleConfig, string>? TaskStateChanged;

        void LoadConfig(AppConfig config);

        void Start();
        void Stop();
        /// <summary>立即手动运行一次（不改变下次计划时间）</summary>
        void RunNow(ScheduleConfig s);
        void RefreshSchedule(Guid id);
        bool KillRunning(Guid id);
    }

    public sealed class SchedulerEngine : ISchedulerEngine
    {
        private AppConfig _config;
        private readonly System.Timers.Timer _timer;
        private readonly object _gate = new();
        private readonly Dictionary<Guid, Process> _running = new();

        public event Action<ScheduleConfig, string>? TaskStateChanged;

        public SchedulerEngine()
        {
            _timer = new System.Timers.Timer(30_000) { AutoReset = true };
            _timer.Elapsed += (_, _) => Tick();
        }

        public void LoadConfig(AppConfig config)
        {
            _config = config;
        }

        public void Start()
        {
            RecalculateAll();
            _timer.Start();
        }

        public void Stop() => _timer.Stop();

        private void RecalculateAll()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var s in _config.Schedules)
                s.NextRunAt = s.ComputeNext(now);
        }

        private void Tick()
        {
            if (!Monitor.TryEnter(_gate)) return;
            try
            {
                var now = DateTimeOffset.UtcNow;
                foreach (var s in _config.Schedules.ToArray())
                {
                    if (!s.Enabled) continue;
                    if (s.NextRunAt is null) { s.NextRunAt = s.ComputeNext(now); continue; }
                    if (s.NextRunAt <= now)
                    {
                        Launch(s);
                        s.NextRunAt = s.ComputeNext(now);
                    }
                }
            }
            finally { Monitor.Exit(_gate); }
        }

        /// <summary>立即手动运行一次（不改变下次计划时间）</summary>
        public void RunNow(ScheduleConfig s)
        {
            if (s.RunningProcessId.HasValue) return;
            Launch(s);
        }

        private void Launch(ScheduleConfig s)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = s.FilePath,
                    Arguments = s.Arguments ?? "",
                    WorkingDirectory = string.IsNullOrWhiteSpace(s.WorkingDirectory)
                        ? Path.GetDirectoryName(s.FilePath) ?? ""
                        : s.WorkingDirectory,
                    UseShellExecute = true
                };
                var p = Process.Start(psi);
                if (p is null) { TaskStateChanged?.Invoke(s, "启动失败"); return; }

                s.RunningProcessId = p.Id;
                lock (_running) { _running[s.Id] = p; }

                p.EnableRaisingEvents = true;
                p.Exited += (_, _) =>
                {
                    lock (_running) { _running.Remove(s.Id); }
                    s.RunningProcessId = null;
                    int code = -1;
                    try { code = p.ExitCode; } catch { }
                    TaskStateChanged?.Invoke(s, $"已结束（ExitCode={code}）");
                    p.Dispose();
                };

                TaskStateChanged?.Invoke(s, $"已启动，PID={p.Id}");
            }
            catch (Exception ex)
            {
                TaskStateChanged?.Invoke(s, $"启动异常：{ex.Message}");
            }
        }

        public bool KillRunning(Guid id)
        {
            lock (_running)
            {
                if (!_running.TryGetValue(id, out var p)) return false;
                try
                {
                    if (!p.HasExited) p.Kill(entireProcessTree: true);
                    return true;
                }
                catch { return false; }
            }
        }

        public void RefreshSchedule(Guid id)
        {
            var s = _config.Schedules.FirstOrDefault(x => x.Id == id);
            if (s is not null) s.NextRunAt = s.ComputeNext(DateTimeOffset.UtcNow);
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
            lock (_running)
            {
                foreach (var p in _running.Values) { try { p.Dispose(); } catch { } }
                _running.Clear();
            }
        }
    }
}
