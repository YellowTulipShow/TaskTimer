public sealed class MainForm : Form
{    private readonly NotifyIcon _tray;
    private readonly Icon _trayIcon;
    public MainForm()
    {
        // 窗体基本属性（等价于设计器里做的事）
        Text = "任务计划程序";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(900, 600);
        MinimumSize = new Size(600, 400);

        string source_name = "YTS.TaskTimer.Assets.app.ico";
        // 设置窗口图标
        Icon = IconLoader.FromEmbedded(source_name);
        // this.Icon = IconLoader.FromFile("Assets/app.ico");

        // 手写控件装配
        var menu = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("文件");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("退出", null, (_, _) => Close()));
        menu.Items.Add(fileMenu);
        Controls.Add(menu);
        MainMenuStrip = menu;


        _trayIcon = IconLoader.FromEmbedded(source_name);
        // _trayIcon = IconLoader.FromFile("Assets/app.ico");
        _tray = new NotifyIcon
        {
            Icon = _trayIcon,
            Text = "任务计划程序",           // 提示文字，≤ 63 字符
            Visible = true
        };

        var ctx = new ContextMenuStrip();
        ctx.Items.Add("显示主窗口", null, (_, _) => RestoreFromTray());
        ctx.Items.Add("退出", null, (_, _) => { _tray.Visible = false; Close(); });
        _tray.ContextMenuStrip = ctx;
        _tray.DoubleClick += (_, _) => RestoreFromTray();
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
        // base.OnClosed(e);
    }
}
