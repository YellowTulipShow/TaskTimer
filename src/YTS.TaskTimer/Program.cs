using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic.Logging;
using YTS.Logic;
using YTS.Logic.Implement;
using YTS.TaskTimer.UIForms;
using YTS.TaskTimer.UITools;

namespace YTS.TaskTimer;

static class Program
{
    [STAThread]
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // 创建互斥体，确保只有一个实例运行
        using Mutex mutex = new(true, ReSourceConst.PowerOnStartupSignName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("应用程序已经在运行中！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return; // 已有实例，退出
        }

        // 1. 初始化 WinForms 默认配置（视觉样式、DPI 等）
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.ToString(), "未处理异常",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        // 2. 构建主机，这是 DI 的基石
        HostApplicationBuilder? builder;
        builder = Host.CreateApplicationBuilder();
        IServiceCollection service = builder.Services;

        // 3. 注册你的服务（详见下文）
        DependencyInjectionImplementationCaseInstallExtend.RegisterExecute(service);
        ConfigureServices(service);

        // 4. 构建主机
        using IHost host = builder.Build();
        IServiceProvider provider = host.Services;

        using ISchedulerEngine engine = provider.GetRequiredService<ISchedulerEngine>();
        engine.Start();

        // 5. 从 DI 容器中解析出主窗体
        //    注意：MainForm 本身也必须被注册（通常是 AddTransient）
        Form mainForm = provider.GetRequiredService<MainForm>();
        // 6. 运行 WinForms 消息循环
        Application.Run(mainForm);

        // 7. 窗体关闭后，优雅地停止主机并释放资源
        host.StopAsync().GetAwaiter().GetResult();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // ====== 在这里集中注册所有服务 ======

        // --- 窗体注册 ---
        // 主窗体通常注册为 Transient，因为桌面应用一般只有一个主窗体实例
        services.AddTransient<MainForm>();

        services.AddSingleton<ISchedulerEngine, SchedulerEngine>();

        // 与自启动相关的服务
        services.AddSingleton<IPowerOnStartup>((sp) =>
        {
            ILog log = sp.GetRequiredService<ILog>();
            return new PowerOnStartupWindow(ReSourceConst.PowerOnStartupSignName, log);
        });


    }
}
