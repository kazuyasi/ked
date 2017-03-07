using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Sprache;

namespace ked
{
    class Program
    {
        static void Main(string[] args)
        {
            ReadOnlyCollection<string> OPTION_HAS_ARG = Array.AsReadOnly(new string[] { "e" });
            List<string> path = new List<string>();
            List<string> option = new List<string>();
            List<string> script = new List<string>();
            List<string> input = new List<string>();

            // args を　入力パス、オプション、スクリプトの3つに分ける。
            if (args.Length != 0)
            {
                bool nextIsOption = false;

                //引数をオプション、パス、スクリプトの３つに分ける。
                foreach (string arg in args)
                {
                    if (arg.StartsWith("-"))
                    {
                        string op = arg.TrimStart("-".ToCharArray()).ToLower();
                        if (OPTION_HAS_ARG.Contains(op)) nextIsOption = true;   //次のargをオプションの引数とする。
                        option.Add(op);
                    }
                    else if (nextIsOption)
                    {
                        if (arg.StartsWith("-")) CautionDosentHaveArg(option);

                        nextIsOption = false;
                    }
                    else if (File.Exists(arg)) path.Add(arg);
                    else if (IsScript(arg)) script.Add(TrimSingleQuotation(arg));
                    else Console.WriteLine("ked detect illegal arg:" + arg);
                }
                if (nextIsOption) CautionDosentHaveArg(option); //オプションがないときは注意。

                Encoding enc = Encoding.GetEncoding("shift-jis");

                // option解釈（特殊編：returnするものは優先度順にこちらへ）
                if (option.Contains("d"))
                {
                    TraceDebugData(path, option, script);

#if DEBUG
                    Console.ReadLine();
#endif

                    return;
                }
                else if (option.Contains("v"))
                {
                    DisplayVersion();

#if DEBUG
                    Console.ReadLine();
#endif
                    return;
                }

                // option解釈（普通編:逐次解釈するものはこちらへ）
                foreach (string op in option.ToArray())
                {
                    switch(op)
                    {
                        case "e":
                            string opArg;   // Option argment
                            if (string.IsNullOrEmpty(opArg = GetOptionArg(option, "e"))) enc = Encoding.GetEncoding(opArg);
                            break;

                        default:
                            Console.WriteLine("{0} is illigall option! :-<");
                            break;
                    }
                }

                // input 抽出
                ExtractInput(path, input, enc);

                if (input.Count == 0)
                {
                    Console.WriteLine("No input! XD");
                    return;
                }
            }
            else
            {
                CautionUsage();
            }
        }

        /// <summary>
        /// バージョンとコピーライトを表示する。
        /// </summary>
        private static void DisplayVersion()
        {
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine(fvi);

            AssemblyCopyrightAttribute aca = (AssemblyCopyrightAttribute)
                Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute));
            Console.WriteLine(aca.Copyright);
        }

        /// <summary>
        /// デバッグ用に引数の分類を出力する。
        /// </summary>
        /// <param name="path">入力パスデータ</param>
        /// <param name="option">オプションデータ</param>
        /// <param name="script">スクリプトデータ</param>
        private static void TraceDebugData(List<string> path, List<string> option, List<string> script)
        {
            Console.WriteLine("********************************** ked DEBUG mode **********************************");


            Console.WriteLine();
            Console.WriteLine("  {0} has {1} data.", nameof(path), path.Count);
            PrintList(path);
            Console.WriteLine();
            Console.WriteLine("  {0} has {1} data.", nameof(option), option.Count);
            PrintList(option);
            Console.WriteLine();
            Console.WriteLine("  {0} has {1} data.", nameof(script), script.Count);
            PrintList(script);
            Console.WriteLine();

            Console.WriteLine("************************************************************************************");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static int PrintList(List<string> list)
        {
            int cnt = 0;
            foreach (string s in list.ToArray())
            {
                Console.WriteLine("    {0:d4} : {1}", cnt++, s);
            }

            return cnt;
        }

        /// <summary>
        /// 頭と末尾にあるシングルクォーテーションをトリムする。
        /// </summary>
        /// <param name="arg">もとの引数</param>
        /// <returns>シングルクォーテーションの中身</returns>
        private static string TrimSingleQuotation(string arg)
        {
            return arg.TrimStart("'".ToCharArray()).TrimEnd("'".ToCharArray());
        }

        /// <summary>
        /// 標準入力と入力パスから入力文字列を抽出する。
        /// </summary>
        /// <param name="path"></param>
        /// <param name="input"></param>
        /// <param name="enc"></param>
        private static void ExtractInput(List<string> path, List<string> input, Encoding enc)
        {
            using (TextReader tr = Console.In)
            {
                while (tr.Peek() != -1)
                {
                    input.Add(tr.ReadLine());
                }
            }

            // pathが存在するならテキストファイルとして指定エンコードで読み込む。
            if (path.Count != 0)
            {
                foreach (string p in path.ToArray())
                {
                    using (StreamReader sr = new StreamReader(p, enc))
                    {
                        while (sr.Peek() != -1)
                        {
                            input.Add(sr.ReadLine());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 使用方法を出力する。
        /// </summary>
        private static void CautionUsage()
        {
            Console.WriteLine("Usage:ked [File path] 'adress-command' [-option]");
        }

        /// <summary>
        /// 指定オプションの引数を取得する。
        /// </summary>
        /// <param name="option"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        private static string GetOptionArg(List<string> option, string op)
        {
            int tmp = option.IndexOf(op);

            if (tmp == -1 || tmp + 1 >= option.Count) return string.Empty;

            return   option[option.IndexOf(op) + 1];
        }

        /// <summary>
        /// 引数が必要なオプションの注意喚起
        /// </summary>
        /// <param name="option">オプション配列</param>
        private static void CautionDosentHaveArg(List<string> option)
        {
            Console.WriteLine("-" + option[option.Count - 1] + " use argument. Plz set argument.");
        }

        /// <summary>
        /// 指定した引数文字列がスクリプトかどうかを判定する。
        /// </summary>
        /// <param name="arg">引数文字列</param>
        /// <returns>スクリプトか否か</returns>
        private static bool IsScript(string arg)
        {
            return arg.StartsWith("'") && arg.EndsWith("'");
        }
    }
}
