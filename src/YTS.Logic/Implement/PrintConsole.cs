namespace YTS.Logic.Implement
{
    /// <summary>
    /// 实现类: 控制台打印展示
    /// </summary>
    public class PrintConsole : IPrint
    {
        /// <inheritdoc/>
        public void PrintLines(params string[] msglist)
        {
            if (msglist == null || msglist.Length <= 0)
                return;
            foreach (string str in msglist)
            {
                Console.WriteLine(str);
            }
        }
    }
}
