
namespace YTS.Logic.Implement
{
    /// <summary>
    /// 连接打印项
    /// </summary>
    internal sealed class ConnectPrint : IPrint
    {
        private readonly IPrint print;
        private readonly IPrint[] prints;

        /// <summary>
        /// 初始化实现
        /// </summary>
        public ConnectPrint(IPrint print, IPrint[] prints)
        {
            this.print = print;
            this.prints = prints;
        }

        public void PrintLines(params string[] msglist)
        {
            print?.PrintLines(msglist);

            for (int i = 0; prints != null && i < prints.Length; i++)
            {
                prints[i].PrintLines(msglist);
            }
        }
    }
}
