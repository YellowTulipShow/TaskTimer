using YTS.Logic;
using YTS.Logic.Implement;
using YTS.Logic.Models;
using YTS.TaskTimer.UITools;

namespace YTS.TaskTimer.UIForms
{
    public sealed class MainForm : Form
    {
        private readonly AppConfig _config;
        private readonly ISchedulerEngine _engine;

        private readonly NotifyIcon _tray;
        private readonly Icon _trayIcon;
        private readonly ListView _taskList;
        private readonly ListView logList;
        private readonly SplitContainer _split;
        private readonly ToolStripStatusLabel _statusLabel;

        private readonly ILog log;
        private readonly IUILogView uILogView;
        private readonly IPowerOnStartup powerOnStartup;


        private bool _reallyExit;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _trayIcon.Dispose();

                uILogView.Dispose();
            }
            base.Dispose(disposing);
        }

        public MainForm(ILog log, IPowerOnStartup powerOnStartup, ISchedulerEngine engine)
        {
            this.log = log;
            this.powerOnStartup = powerOnStartup;

            _config = ConfigManageExtend.ReadConfigFileContent();
            _engine = engine;
            _engine.LoadConfig(_config);

            Form self = this;
            FormStyleConfig(self);

            // 设置窗口图标
            self.Icon = IconLoader.FromEmbedded(ReSourceConst.SystemIconReSourceName);

            // 日志展示
            uILogView = new LogListView();
            logList = uILogView.GetListView();
            logList.Dock = DockStyle.Fill;
            self.Controls.Add(logList);

            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                Panel1MinSize = 100,
                Panel2MinSize = 100,
            };
            _taskList = UICreateTaskList();
            _split.Panel1.Controls.Add(_taskList);
            _split.Panel2.Controls.Add(logList);

            var status = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("就绪");
            status.Items.Add(_statusLabel);

            // 创建菜单栏工具
            MenuStrip menu = UICreateMenuStrip();
            menu.Dock = DockStyle.Top;
            self.MainMenuStrip = menu;

            var toolbar = BuildToolbar();

            // 创建通知小图标
            _trayIcon = IconLoader.FromEmbedded(ReSourceConst.SystemIconReSourceName);
            _tray = UICreateNotifyIcon(_trayIcon);

            Controls.Add(_split);
            Controls.Add(toolbar);
            Controls.Add(menu);
            Controls.Add(status);

            // 引擎事件
            _engine.TaskStateChanged += OnEngineTaskStateChanged;

            FormClosing += OnFormClosing;
            Resize += (_, _) =>
            {
                if (WindowState == FormWindowState.Minimized && _config.CloseToTray)
                {
                    Hide();
                    ShowInTaskbar = false;
                }
                SafeSetSplitterDistance(_split, 460);
            };
            Shown += (_, _) =>
            {
                // try { _split.SplitterDistance = Math.Min(460, _split.Width - 200); } catch { }
                UpdateStatusBar();
                SafeSetSplitterDistance(_split, 460);
            };

            RefreshTaskList();
            uILogView.WriteLog($"[系统]程序启动，共 {_config.Schedules.Count} 个任务");
        }
        private static void SafeSetSplitterDistance(SplitContainer split, int desired)
        {
            // 控件还没布局好，直接跳过，等下次 Resize / Shown 再设
            if (split.Width <= 0) return;

            int min = split.Panel1MinSize;
            int max = split.Width - split.Panel2MinSize - split.SplitterWidth;

            // 宽度太小，容不下两个 MinSize，放弃设置
            if (max < min) return;

            split.SplitterDistance = Math.Clamp(desired, min, max);
        }
        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_reallyExit) return;

            if (e.CloseReason == CloseReason.UserClosing && _config.CloseToTray)
            {
                e.Cancel = true;
                Hide();
                ShowInTaskbar = false;
                _tray.ShowBalloonTip(2000, "定时任务管理器",
                    "已最小化到托盘，任务继续运行", ToolTipIcon.Info);
            }
        }
        private static void FormStyleConfig(Form self)
        {
            // 窗体基本属性（等价于设计器里做的事）
            self.Text = "任务计划程序";
            self.StartPosition = FormStartPosition.CenterScreen;
            self.Size = new Size(900, 600);
            self.MinimumSize = new Size(600, 400);
        }
        private FlowLayoutPanel BuildToolbar()
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(8, 8, 8, 0)
            };
            bar.Controls.Add(MakeButton("新建", (_, _) => OnNewTask()));
            bar.Controls.Add(MakeButton("编辑", (_, _) => OnEditTask()));
            bar.Controls.Add(MakeButton("删除", (_, _) => OnDeleteTask()));
            bar.Controls.Add(MakeButton("立即运行", (_, _) => OnRunTask()));
            bar.Controls.Add(MakeButton("终止运行", (_, _) => OnKillTask()));
            // bar.Controls.Add(MakeButton("清空日志", (_, _) => ClearLogs()));
            return bar;
        }
        private static Button MakeButton(string text, EventHandler onClick)
        {
            var b = new Button { Text = text, Size = new Size(88, 28), Margin = new Padding(0, 0, 6, 0) };
            b.Click += onClick;
            return b;
        }

        private ListView UICreateTaskList()
        {
            var lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = false,
                HideSelection = false
            };
            lv.Columns.Add("任务名称", 150);
            lv.Columns.Add("下次执行", 160);
            lv.Columns.Add("状态", 120);
            lv.Columns.Add("脚本路径", 320);
            lv.DoubleClick += (_, _) => OnEditTask();
            return lv;
        }

        private void OnNewTask()
        {
            var cfg = new ScheduleConfig { Name = "新任务", Cron = "0 9 * * *" };
            using var f = new TaskEditForm(cfg);
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                _config.Schedules.Add(f.Result);
                _engine.RefreshSchedule(f.Result.Id);
                ConfigManageExtend.SaveConfigFileContent(_config);
                RefreshTaskList();
                uILogView.WriteLog($"[系统]新增任务「{f.Result.Name}」");
            }
        }

        private void OnEditTask()
        {
            var s = GetSelectedSchedule();
            if (s is null) { SetStatus("请先选择一个任务"); return; }

            using var f = new TaskEditForm(s.Clone());
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                s.Name = f.Result.Name;
                s.FilePath = f.Result.FilePath;
                s.Arguments = f.Result.Arguments;
                s.WorkingDirectory = f.Result.WorkingDirectory;
                s.Cron = f.Result.Cron;
                s.Enabled = f.Result.Enabled;
                _engine.RefreshSchedule(s.Id);
                ConfigManageExtend.SaveConfigFileContent(_config);
                RefreshTaskList();
                uILogView.WriteLog($"[系统]修改任务「{s.Name}」");
            }
        }

        private void OnDeleteTask()
        {
            var s = GetSelectedSchedule();
            if (s is null) return;

            if (s.RunningProcessId.HasValue &&
                MessageBox.Show(this, "该任务正在运行，先终止并删除？", "确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            if (s.RunningProcessId.HasValue) _engine.KillRunning(s.Id);

            if (MessageBox.Show(this, $"删除任务「{s.Name}」？", "确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _config.Schedules.Remove(s);
            ConfigManageExtend.SaveConfigFileContent(_config);
            RefreshTaskList();
            uILogView.WriteLog($"[系统]删除任务「{s.Name}」");
        }

        private void OnRunTask()
        {
            var s = GetSelectedSchedule();
            if (s is null) { SetStatus("请先选择一个任务"); return; }
            _engine.RunNow(s);
        }

        private void OnKillTask()
        {
            var s = GetSelectedSchedule();
            if (s is null) return;
            KillTask(s.Id);
        }

        private void KillTask(Guid id)
        {
            var s = _config.Schedules.FirstOrDefault(x => x.Id == id);
            if (s is null || !s.RunningProcessId.HasValue) return;
            if (_engine.KillRunning(id))
                uILogView.WriteLog($"[{s.Name}]已手动终止", EUILogViewType.WARN);
        }
        private ScheduleConfig? GetSelectedSchedule()
        {
            if (_taskList.SelectedIndices.Count == 0) return null;
            var idx = _taskList.SelectedIndices[0];
            if (idx < 0 || idx >= _config.Schedules.Count) return null;
            return _config.Schedules[idx];
        }
        private void SetStatus(string text) => _statusLabel.Text = text;
        private void OnEngineTaskStateChanged(ScheduleConfig s, string message)
        {
            EUILogViewType level = message.Contains("异常") || message.Contains("失败") ?
                EUILogViewType.ERROR : EUILogViewType.INFO;
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(() =>
                {
                    string msg = $"任务[{s.Name}]: {message}";
                    uILogView.WriteLog(msg, level);
                    RefreshTaskList();
                    UpdateStatusBar();
                });
            }
        }
        private void RefreshTaskList()
        {
            if (_taskList.IsDisposed) return;
            int selected = _taskList.SelectedIndices.Count > 0 ? _taskList.SelectedIndices[0] : -1;

            _taskList.BeginUpdate();
            _taskList.Items.Clear();
            foreach (var s in _config.Schedules)
            {
                var next = s.NextRunAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "—";
                var status = s.RunningProcessId.HasValue
                    ? $"运行中 PID={s.RunningProcessId}"
                    : (s.Enabled ? "已启用" : "已禁用");

                var item = new ListViewItem(s.Name);
                item.SubItems.Add(next);
                item.SubItems.Add(status);
                item.SubItems.Add(s.FilePath);

                if (!s.Enabled) item.ForeColor = Color.Gray;
                else if (s.RunningProcessId.HasValue) item.ForeColor = Color.SeaGreen;

                _taskList.Items.Add(item);
            }
            _taskList.EndUpdate();

            if (selected >= 0 && selected < _taskList.Items.Count)
                _taskList.Items[selected].Selected = true;
        }
        private void UpdateStatusBar()
        {
            int total = _config.Schedules.Count;
            int enabled = _config.Schedules.Count(x => x.Enabled);
            int running = _config.Schedules.Count(x => x.RunningProcessId.HasValue);
            _statusLabel.Text = $"共 {total} 个任务，启用 {enabled}，运行中 {running}";
        }

        private ToolStripMenuItem UICreateToolStripMenuItem_System()
        {
            ToolStripMenuItem m = new("系统");
            m.DropDownItems.Add(new ToolStripMenuItem("退出", null, (_, _) => Close()));
            return m;
        }
        private ToolStripMenuItem UICreateToolStripMenuItem_Tools()
        {
            ToolStripMenuItem m = new("工具");

            // 开机自启动
            {
                // 创建一个可勾选的菜单项
                ToolStripMenuItem autoStartMenuItem = new("开机自启动")
                {
                    CheckOnClick = true,
                    // 初始化时，根据当前注册表状态设置勾选状态
                    Checked = powerOnStartup.IsAutoStartEnabled()
                };

                // 处理点击事件
                autoStartMenuItem.Click += (_, _) =>
                {
                    if (autoStartMenuItem.Checked)
                    {
                        // 用户勾选，尝试启用
                        if (!powerOnStartup.Enable())
                        {
                            // 如果启用失败，回滚勾选状态
                            autoStartMenuItem.Checked = false;
                        }
                    }
                    else
                    {
                        // 用户取消勾选，尝试禁用
                        if (!powerOnStartup.Disable())
                        {
                            // 如果禁用失败，回滚勾选状态
                            autoStartMenuItem.Checked = true;
                        }
                    }
                };

                m.DropDownItems.Add(autoStartMenuItem);
            }
            return m;
        }
        private MenuStrip UICreateMenuStrip()
        {
            // 手写控件装配
            MenuStrip menu = new();

            menu.Items.Add(UICreateToolStripMenuItem_System());
            menu.Items.Add(UICreateToolStripMenuItem_Tools());

            return menu;
        }

        private NotifyIcon UICreateNotifyIcon(Icon icon)
        {
            NotifyIcon t = new()
            {
                Icon = icon,
                Text = "任务计划程序",           // 提示文字，≤ 63 字符
                Visible = true
            };

            ContextMenuStrip ctx = new();
            ctx.Items.Add("显示主窗口", null, (_, _) => RestoreFromTray());
            ctx.Items.Add("退出", null, (_, _) => { t.Visible = false; Close(); });
            t.ContextMenuStrip = ctx;
            t.DoubleClick += (_, _) => RestoreFromTray();
            return t;
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }
        private void ExitApplication()
        {
            if (MessageBox.Show(this, "确定退出程序？所有正在运行的子进程会被终止。", "确认退出",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _reallyExit = true;
            uILogView.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _engine.TaskStateChanged -= OnEngineTaskStateChanged;
            _engine.Dispose();

            Close();
            Application.ExitThread();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
                Hide();   // 配合 NotifyIcon 实现最小化到托盘
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
