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
    class Script
    {
        public Address Address;
        public Command Command;

        public Script(Address a, Command cmd)
        {
            this.Address = a;
            this.Command = cmd;
        }

        public string ExtractCommand(string script)
        {
            return script.Substring(this.Address.ToString().Length);
        }
    }

    class Address
    {
        public string Start { private set; get; }
        public OptionAddress Option { private set; get; }
        public bool Not { private set; get; }

        public Address(string st)
        {
            this.Start = st;
            this.Option = new OptionAddress();
            this.Not = false;
        }

        public Address(string st, char n)
        {
            this.Start = st;
            this.Option = new OptionAddress();
            this.Not = (n == '!');
        }

        public Address(string st, OptionAddress oa, string n)
        {
            this.Start = st;
            this.Option = oa;
            this.Not = (n == "!");
        }

        public Address(string st, char v, string o, char n)
        {
            this.Start = st;
            this.Option = new OptionAddress(v, o);
            this.Not = (n == '!');
        }

        /// <summary>
        /// Verbが０のとき空文字列を出力するように訂正
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return 
                this.Start + 
                ( (this.Option.Verb == 0)? "" : this.Option.Verb.ToString() ) +  
                this.Option.Object + 
                (this.Not? "!": "");
        }

        public string ExtractCommand(string script)
        {
            return script.Substring(this.ToString().Length);
        }

        public int[] GetIndex(List<string> input)
        {
            List<int> ret = new List<int>();
            int end = input.Count - 1;

            if (Start == "$") ret.Add(end);
            if (Start != "$" && Option.Verb == 0) ret.Add(int.Parse(Start));
            if (Start != "$" && Option.Verb == ',')
            {
                int endidx = (Option.Object == "$") ? end : int.Parse(Option.Object);

                for (int i = int.Parse(Start); i <= endidx; i++)
                {
                    ret.Add(i);
                }
            }
            if (Start != "$" && !Not && Option.Verb == '~')
            {
                if (Option.Object == "$") throw new ArgumentException();


                int step = int.Parse(Option.Object);

                for(int i = int.Parse(Start); i <= end; i += step)
                {
                    ret.Add(i);
                }
            }

            //Notがあるなら否定で返す。
            if(Not)
            {
                List<int> tmp = new List<int>();

                //全集合
                for (int i = 0; i <= end; i++)
                {
                    tmp.Add(i);
                }

                //今まで入力した分を削除
                foreach (int i in ret.ToArray())
                {
                    tmp.Remove(i);
                }

                ret = tmp;
            }

            return ret.ToArray();
        }
    }

    class OptionAddress
    {
        public char Verb { private set; get; }
        public string Object { private set; get; }

        public OptionAddress()
        {

        }

        public OptionAddress(char v, string o)
        {
            this.Verb = v;
            this.Object = o;
        }
    }

    class Command
    {
        public char Operation { private set; get; }
        public string TargetText { private set; get; }
        public string ReplaceText { private set; get; }

        public Command()
        {

        }

        public Command(char op)
        {
            this.Operation = op;
        }

        public Command(char op, string text1, string text2)
        {
            this.Operation = op;
            this.TargetText = text1;
            this.ReplaceText = text2;
        }

        public override string ToString()
        {
            return (this.Operation == 's')? 
                this.Operation + "/" + this.TargetText + "/" + this.ReplaceText + "/" :
                this.Operation.ToString();
        }

        
    }

    class Program
    {
        static readonly Parser<string> digit = Parse.Digit.AtLeastOnce().Text();
        static readonly Parser<IEnumerable<string>> digits = digit.Many();

        static readonly Parser<string> Text = Parse.Letter.AtLeastOnce().Text();

        /// <summary>
        /// Addressとなる数字か＄をパースするパーサ。
        /// "100$"などは正常にパースしてしまう……。警告できない。
        /// </summary>
        static readonly Parser<string> AddressNumber = digit.Or(Parse.String("$").Text());

        static readonly Parser<OptionAddress> OptionAddress =
            from commma in Parse.Char(',').Or(Parse.Char('~'))
            from argNum in AddressNumber
            select new OptionAddress(commma, argNum);

        static readonly Parser<Address> AddressText =
            from startAddress in AddressNumber
            from option in OptionAddress.XOr<OptionAddress>(Parse.Return(new OptionAddress()))
            from not in Parse.String("!").Text().XOr(Parse.Return(""))
            select new Address(startAddress, option, not);

        /// <summary>
        /// 引数を取らないコマンドのパーサー。ArgumentCommandとOrを取るとエラー
        /// </summary>
        static readonly Parser<Command> NoArgumentCommand =
            from cmd in Parse.Char('d').Or(Parse.Char('p')).End()
            select new Command(cmd);

        /// <summary>
        /// 引数を取るコマンドのパーサー。NoArgumentCommandとOrを取るとエラー
        /// </summary>
        static readonly Parser<Command> ArgumentCommand =
            from cmd in Parse.Char('s')
            from slash1 in Parse.Char('/')
            from text1 in Text
            from slash2 in Parse.Char('/')
            from text2 in Text.XOr(Parse.Return(""))
            from slash3 in Parse.Char('/').End()
            select new Command(cmd, text1, text2);


        static void Main(string[] args)
        {
            ReadOnlyCollection<string> OPTION_HAS_ARG = Array.AsReadOnly(new string[] { "e" });
            List<string> path = new List<string>();
            List<string> option = new List<string>();
            List<string> script = new List<string>();
            List<string> input = new List<string>();
            List<string> patternSpace = new List<string>();

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

                        option.Add(arg);
                        nextIsOption = false;
                    }
                    else if (File.Exists(arg)) path.Add(arg);
                    else if (IsScript(arg)) script.Add(TrimSingleQuotation(arg));
                    else script.Add(arg);
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

                bool silentMode = false;

                // option解釈（普通編:逐次解釈するものはこちらへ）
                bool nextLoopSkip = false;
                foreach (string op in option.ToArray())
                {
                    if (nextLoopSkip)
                    {
                        nextLoopSkip = false;
                        continue;
                    }
                    switch(op)
                    {
                        case "e":
                            string opArg;   // Option argment
                            if (!string.IsNullOrEmpty(opArg = GetOptionArg(option, "e"))) enc = Encoding.GetEncoding(opArg);
                            nextLoopSkip = true;    //次はオプションなのでスキップする。
                            break;

                        case "n":
                            silentMode = true;
                            break;

                        default:
                            Console.WriteLine("{0} is illigall option! :-<", op);
                            break;
                    }
                }

                Console.OutputEncoding = enc;

                // input 抽出
                ExtractInput(path, input, enc);

                if (input.Count == 0)
                {
                    Console.WriteLine("No input! XD");
                    return;
                }

                // パターンスペースへコピー,サイレントモードなら空のまま
                if (!silentMode) patternSpace = input;

                //スクリプトを逐次実行
                foreach (string st in script.ToArray())
                {
                    // 例外が発生するのでAddress→引数なしコマンド→引数有りコマンドの順にパースする
                    var adresult = AddressText.TryParse(st);

                    if(adresult.WasSuccessful)
                    {
                        var ad = adresult.Value;

                        var ct = ad.ExtractCommand(st);

                        var cmdresult = NoArgumentCommand.TryParse(ct);
                        if (!cmdresult.WasSuccessful) cmdresult = ArgumentCommand.TryParse(ct);

                        if(cmdresult.WasSuccessful)
                        {
                            var cmd = cmdresult.Value;

                            Script sc = new Script(ad, cmd);

                            int[] idxs = sc.Address.GetIndex(input);
                            switch (sc.Command.Operation)
                            {
                                case 'd':
                                    //削除は逆順に行い、順番が乱れないようにする。
                                    for (int i = 0; i < idxs.Length; i++)
                                    {
                                        patternSpace.RemoveAt(idxs[idxs.Length - 1 - i]);
                                    }
                                    break;

                                case 'p':
                                    for (int i = 0; i < idxs.Length; i++)
                                    {
                                        patternSpace.Add(input[idxs[i]]);
                                    }
                                    break;

                                case 's':
                                    for (int i = 0; i < idxs.Length; i++)
                                    {
                                        patternSpace[i] = patternSpace[i].Replace(sc.Command.TargetText, sc.Command.ReplaceText);
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }

                StringBuilder sb = new StringBuilder();
                foreach (string line in patternSpace.ToArray())
                {
                    sb.AppendLine(line);
                }

                Console.Write(sb);
#if DEBUG
                Console.ReadLine();
#endif
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
            else
            {
                using (TextReader tr = Console.In)
                {
                    string line;
                    while ((line = tr.ReadLine()) != null)
                    {
                        input.Add(line);
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

            string ret = option[option.IndexOf(op) + 1];
            option.Remove(ret);
            return ret;
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
