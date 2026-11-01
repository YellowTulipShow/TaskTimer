using yts.TaskTimer.UITools;

using YTS.Logic.Func;
using YTS.Logic.Func.Implement;

namespace YTS.TaskTimer.UIForms
{
    public sealed class MainForm : Form
    {
        private readonly NotifyIcon _tray;
        private readonly Icon _trayIcon;
        public MainForm()
        {
            Form self = this;
            // 窗体基本属性（等价于设计器里做的事）
            self.Text = "任务计划程序";
            self.StartPosition = FormStartPosition.CenterScreen;
            self.Size = new Size(900, 600);
            self.MinimumSize = new Size(600, 400);

            string source_name = "YTS.TaskTimer.Assets.app.ico";
            // 设置窗口图标
            self.Icon = IconLoader.FromEmbedded(source_name);

            // 创建菜单栏工具
            MenuStrip menu = UICreateMenuStrip();
            self.Controls.Add(menu);
            self.MainMenuStrip = menu;

            // 创建通知小图标
            _trayIcon = IconLoader.FromEmbedded(source_name);
            _tray = UICreateNotifyIcon(_trayIcon);
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
                IPowerOnStartup powerOnStartup = new PowerOnStartupWindow("MyTaskScheduler");
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
                    // 用户勾选，尝试启用
                    // 如果启用失败，回滚勾选状态
                    if (autoStartMenuItem.Checked && !powerOnStartup.Enable())
                    {
                        autoStartMenuItem.Checked = false;
                    }
                    // 用户取消勾选，尝试禁用
                    // 如果禁用失败，回滚勾选状态
                    else if (!powerOnStartup.Disable())
                    {
                        autoStartMenuItem.Checked = true;
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
            }
            base.Dispose(disposing);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
