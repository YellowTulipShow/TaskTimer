using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace YTS.Logic.Implement
{
    [SupportedOSPlatform("windows")]
    public class PowerOnStartupWindow : IPowerOnStartup
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private readonly string AppRegistryName;
        private readonly ILog log;
        public PowerOnStartupWindow(string AppRegistryName, ILog log)
        {
            this.AppRegistryName = $"{AppRegistryName}_AutoStart";
            this.log = log;
        }

        // 统一入口：优先 Environment.ProcessPath，兜底老 API
        private string GetExecutablePath()
        {
            // .NET 6+，单文件发布也正确
            string? path = Environment.ProcessPath;

            // 极端环境兜底（例如某些受限沙箱）
            if (string.IsNullOrEmpty(path))
                path = Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("无法获取当前可执行文件路径");

            return path;
        }

        public bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                if (key == null) return false;
                var value = key.GetValue(AppRegistryName) as string;
                return !string.IsNullOrEmpty(value)
                    && string.Equals(value.Trim('"'), GetExecutablePath(), StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public bool Enable()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
                             ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
                key!.SetValue(AppRegistryName, $"\"{GetExecutablePath()}\"");
                log.Info("启用自启动成功");
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"启用自启动失败: {ex.Message}", ex);
            }
        }

        public bool Disable()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                key?.DeleteValue(AppRegistryName, false);
                log.Info("禁用自启动成功");
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"禁用自启动失败: {ex.Message}", ex);
            }
        }
    }
}
