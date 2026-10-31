using System.Drawing;
using System.Reflection;
using System.IO;

namespace yts.TaskTimer.UITools
{
    public static class IconLoader
    {
        /// <summary>
        /// 从嵌入资源流加载
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static Icon FromEmbedded(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using Stream? s = asm.GetManifestResourceStream(resourceName)
                              ?? throw new FileNotFoundException($"未找到嵌入资源: {resourceName}");
            // Icon(Stream) 不会复制数据，需克隆以脱离 Stream 生命周期
            using var tmp = new Icon(s);
            return (Icon)tmp.Clone();
        }

        /// <summary>
        /// 从文件加载
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Icon FromFile(string path)
        {
            // Icon(string) 内部会读取文件，但为了明确生命周期，同样克隆
            using var tmp = new Icon(path);
            return (Icon)tmp.Clone();
        }

        /// <summary>
        /// 从 EXE 自身的图标加载（任务计划程序常用兜底）
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Icon FromExe()
        {
            string exe = Environment.ProcessPath
                         ?? throw new InvalidOperationException("无法获取 exe 路径");
            return Icon.ExtractAssociatedIcon(exe)
                   ?? SystemIcons.Application;
        }
    }
}
