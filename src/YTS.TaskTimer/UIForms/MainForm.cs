using YTS.Logic;
using YTS.TaskTimer.UITools;

namespace YTS.TaskTimer.UIForms
{
    public sealed class MainForm : Form
    {
        private readonly NotifyIcon _tray;
        private readonly Icon _trayIcon;
        private readonly ListView logList;

        private readonly ILog log;
        private readonly IUILogView uILogView;
        private readonly IPowerOnStartup powerOnStartup;
        public MainForm(ILog log, IPowerOnStartup powerOnStartup)
        {
            this.log = log;
            this.powerOnStartup = powerOnStartup;

            Form self = this;
            FormStyleConfig(self);

            // 设置窗口图标
            self.Icon = IconLoader.FromEmbedded(ReSourceConst.SystemIconReSourceName);

            // 日志展示
            uILogView = new LogListView();
            logList = uILogView.GetListView();
            logList.Dock = DockStyle.Fill;
            self.Controls.Add(logList);

            // 创建菜单栏工具
            MenuStrip menu = UICreateMenuStrip();
            menu.Dock = DockStyle.Top;
            self.Controls.Add(menu);
            self.MainMenuStrip = menu;

            // 创建通知小图标
            _trayIcon = IconLoader.FromEmbedded(ReSourceConst.SystemIconReSourceName);
            _tray = UICreateNotifyIcon(_trayIcon);

            uILogView.WriteLog("测试1");
            uILogView.WriteLog("测试1");
            uILogView.WriteLog("测试1");
            uILogView.WriteLog("测试1", EUILogViewType.INFO);
            uILogView.WriteLog("测试1");
            uILogView.WriteLog("测试1");
            uILogView.WriteLog("测试1");
            uILogView.WriteLog("测试1", EUILogViewType.ERROR);
            uILogView.WriteLog(@"测试1\nlfwliejfijwe理解饿了季节理解而为了房间里玩金额累计\n服务金额老骥伏枥忘记了叫我雷锋家里价位附近");
            uILogView.WriteLog("测试1");
            uILogView.WriteLog("测试1", EUILogViewType.WARN);
            uILogView.WriteLog("测试1");
            uILogView.WriteLog("测试1", EUILogViewType.DEBUG);
        }
        private static void FormStyleConfig(Form self)
        {
            // 窗体基本属性（等价于设计器里做的事）
            self.Text = "任务计划程序";
            self.StartPosition = FormStartPosition.CenterScreen;
            self.Size = new Size(900, 600);
            self.MinimumSize = new Size(600, 400);
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
                Hide();   // 配合 NotifyIcon 实现最小化到托盘
        }

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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
