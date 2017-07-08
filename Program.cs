using System;
using System.IO;
using static System.Console;

namespace PCSCPacker
{
    class Program
    {
        public static float size = 0f;
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                WriteLine("将pak文件拖动到程序图标上以解包");
                WriteLine("将文件夹拖动到程序图标上以打包");
            }
            else
            {
                string text = args[0];
                if (Directory.Exists(text))
                {
                    PAK_Edit.PAK(text);
                }
                else
                {
                    WriteLine("是否批量改变模型大小？");
                    if (!float.TryParse(ReadLine(), out size) || size < 0f || size > 255f)
                    {
                        size = 0f;
                    }
                    PAK_Edit.unPAK(text);
                }
            }
            //Console.WriteLine("0.5秒后自动关闭......");
            System.Threading.Thread.Sleep(500);
            //Console.WriteLine("按任意键退出...");
            //Console.ReadKey();
        }
    }
}
