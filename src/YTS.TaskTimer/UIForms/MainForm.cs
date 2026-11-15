using YTS.Logic;
using YTS.Logic.Implement;
using YTS.Logic.Models;
using YTS.TaskTimer.UITools;
using YTS.TaskTimer.UITools.Editors;

namespace YTS.TaskTimer.UIForms
{
    public sealed class MainForm : Form, ISchedulerEngineCallback
    {
        private readonly AppConfig _config;
        private readonly ISchedulerEngine _engine;

        private readonly NotifyIcon _tray;
        private readonly Icon _trayIcon;
        private readonly ListView _taskList;
        private readonly SplitContainer _split;
        private readonly ToolStripStatusLabel _statusLabel;

        private readonly ILog log;
        private readonly IUILogView uILogView;
        private readonly IPowerOnStartup powerOnStartup;
        private readonly IAppConfigManage configManage;

        private bool _reallyExit;
        private IDisposable? dis_SchedulerEngine_call;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _trayIcon.Dispose();

                uILogView.Dispose();
                dis_SchedulerEngine_call?.Dispose();
                dis_SchedulerEngine_call = null;
                _engine.Dispose();
            }
            base.Dispose(disposing);
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
            dis_SchedulerEngine_call?.Dispose();
            dis_SchedulerEngine_call = null;
            _engine.Dispose();

            Close();
            Application.ExitThread();
        }

        public MainForm(ILog log, IPowerOnStartup powerOnStartup, ISchedulerEngine engine, IAppConfigManage configManage)
        {
            this.log = log;
            this.powerOnStartup = powerOnStartup;
            this.configManage = configManage;

            _config = configManage.Read();
            _engine = engine;
            dis_SchedulerEngine_call = _engine.RegisterCallback(this);

            Form self = this;
            FormStyleConfig(self);

            // 设置窗口图标
            self.Icon = IconLoader.FromEmbedded(ReSourceConst.SystemIconReSourceName);

            // 日志展示
            uILogView = new LogListView(configManage);
            Control logControl = uILogView.GetLogControl();
            logControl.Dock = DockStyle.Fill;
            self.Controls.Add(logControl);

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
            _split.Panel2.Controls.Add(logControl);

            var status = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("就绪");
            status.Items.Add(_statusLabel);

            // 创建菜单栏工具
            MenuStrip menu = BuildMenu();
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

            FormClosing += OnFormClosing;
            Resize += (_, _) =>
            {
                if (WindowState == FormWindowState.Minimized && _config.System.CloseToTray)
                {
                    BeginInvoke(HideToTray);
                }
                SafeSetSplitterDistance(_split, 460);
            };
            Shown += (_, _) =>
            {
                UpdateStatusBar();
                SafeSetSplitterDistance(_split, 460);
            };

            RefreshTaskList();
            uILogView.WriteLog($"[系统]程序启动，共 {_config.Schedules.Count} 个任务");
        }

        private void ChangeConfig()
        {
            configManage.Save(_config);
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

            if (e.CloseReason == CloseReason.UserClosing && _config.System.CloseToTray)
            {
                e.Cancel = true;
                BeginInvoke(HideToTray);
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
                ChangeConfig();
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
                ChangeConfig();
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
            ChangeConfig();
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

        private MenuStrip BuildMenu()
        {
            var menu = new MenuStrip();

            var file = new ToolStripMenuItem("文件");
            {
                file.DropDownItems.Add(new ToolStripMenuItem("新建任务", null, (_, _) => OnNewTask())
                {
                    ShortcutKeys = Keys.Control | Keys.N
                });
                file.DropDownItems.Add(new ToolStripMenuItem("保存配置", null, (_, _) =>
                {
                    ChangeConfig();
                    SetStatus("配置已保存");
                })
                {
                    ShortcutKeys = Keys.Control | Keys.S
                });
                file.DropDownItems.Add(new ToolStripMenuItem("编辑系统配置", null, (_, _) =>
                {
                    AppConfigSystem? result = GenericEditorForm<AppConfigSystem>.Edit(_config.System);
                    if (result != null)
                    {
                        _config.System = result;
                        configManage.Save(_config);
                        ChangeConfig();
                        SetStatus("系统配置已保存");
                    }
                })
                {
                    ShortcutKeys = Keys.Control | Keys.C
                });
                file.DropDownItems.Add(new ToolStripMenuItem("编辑日志配置", null, (_, _) =>
                {
                    AppConfigLogControl? result = GenericEditorForm<AppConfigLogControl>.Edit(_config.LogConfig);
                    if (result != null)
                    {
                        _config.LogConfig = result;
                        configManage.Save(_config);
                        ChangeConfig();
                        SetStatus("日志配置已保存");
                    }
                })
                {
                    ShortcutKeys = Keys.Control | Keys.L
                });

                void _test_config<T>() where T : class, new()
                {
                    T m = new();
                    var result = GenericEditorForm<T>.Edit(m);
                    var logArgs = log.CreateArgDictionary();
                    logArgs["m"] = m;
                    log.Info("测试配置编辑", logArgs);
                }
                file.DropDownItems.Add(new ToolStripMenuItem("编辑测试配置.SimpleTestModel", null, (_, _) =>
                {
                    _test_config<UITools.Editors.TestModels.SimpleTestModel>();
                }));
                file.DropDownItems.Add(new ToolStripMenuItem("编辑测试配置.FullTestModel", null, (_, _) =>
                {
                    _test_config<UITools.Editors.TestModels.FullTestModel>();
                }));
                file.DropDownItems.Add(new ToolStripMenuItem("编辑测试配置.LargeTestModel", null, (_, _) =>
                {
                    _test_config<UITools.Editors.TestModels.LargeTestModel>();
                }));

                file.DropDownItems.Add(new ToolStripSeparator()); // 工具栏分隔符
                file.DropDownItems.Add(new ToolStripMenuItem("退出", null, (_, _) => ExitApplication()));
            }
            var tools = new ToolStripMenuItem("工具");
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

                tools.DropDownItems.Add(autoStartMenuItem);
            }

            // 关闭时最小化到托盘
            {
                var closeToTray = new ToolStripMenuItem("关闭时最小化到托盘")
                {
                    CheckOnClick = true,
                    Checked = _config.System.CloseToTray
                };
                closeToTray.Click += (_, _) =>
                {
                    _config.System.CloseToTray = closeToTray.Checked;
                    ChangeConfig();
                };
                tools.DropDownItems.Add(closeToTray);
            }

            menu.Items.Add(file);
            menu.Items.Add(tools);
            return menu;
        }
        private bool _isHidingToTray = false;
        private FormWindowState _stateBeforeHide = FormWindowState.Normal;

        private void HideToTray()
        {
            if (_isHidingToTray) return;
            _isHidingToTray = true;
            try
            {
                // 记录用户隐藏前的状态（排除 Minimized，因为最小化后我们会主动隐藏）
                if (WindowState != FormWindowState.Minimized)
                    _stateBeforeHide = WindowState;

                if (WindowState != FormWindowState.Normal)
                    WindowState = FormWindowState.Normal;

                Hide();
                ShowInTaskbar = false;
                _tray.ShowBalloonTip(2000, "定时任务管理器",
                    "已最小化到托盘，任务继续运行", ToolTipIcon.Info);
            }
            finally { _isHidingToTray = false; }
        }

        private void ShowMainWindow()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = _stateBeforeHide;   // ★ 恢复到隐藏前的状态
            Activate();
        }


        private NotifyIcon UICreateNotifyIcon(Icon icon)
        {
            NotifyIcon t = new()
            {
                Icon = icon,
                Text = "任务计划程序",           // 提示文字，≤ 63 字符
                Visible = true
            };

            var ctx = new ContextMenuStrip();
            ctx.Items.Add("打开主界面", null, (_, _) => ShowMainWindow());
            ctx.Items.Add("退出", null, (_, _) => ExitApplication());
            ctx.Opening += (_, _) =>
            {
                for (int i = ctx.Items.Count - 1; i >= 0; i--)
                    if (ctx.Items[i].Tag is Guid) ctx.Items.RemoveAt(i);

                var running = _config.Schedules.Where(s => s.RunningProcessId.HasValue).ToList();
                if (running.Count == 0) return;

                int insertAt = 1;
                ctx.Items.Insert(insertAt++, new ToolStripSeparator());
                foreach (var s in running)
                {
                    var item = new ToolStripMenuItem($"终止「{s.Name}」") { Tag = s.Id };
                    item.Click += (_, _) => KillTask(s.Id);
                    ctx.Items.Insert(insertAt++, item);
                }
            };

            t.ContextMenuStrip = ctx;
            t.DoubleClick += (_, _) => ShowMainWindow();
            return t;
        }

        public void OnLog(string text)
        {
            uILogView.WriteLog(text);
        }

        public void OnTaskStateChanged(ScheduleConfig schedule, string text)
        {
            uILogView.WriteLog($"任务[{schedule.Name}]状态更改: {text}");
        }
    }
}
