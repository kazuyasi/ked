using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Sprache;
using System.Text.RegularExpressions;
using Hnx8.ReadJEnc;

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

        /// <summary>
        /// 対象のアドレス指定子をインデックスに変換する
        /// </summary>
        /// <param name="target">アドレス指定子</param>
        /// <param name="input">入力文字列</param>
        /// <returns>インデックス</returns>
        private int[] ConvertToIndex(string target, List<string> input)
        {
            if(string.IsNullOrEmpty(target)) return new int[] { -1 };

            if (target == "$") return new int[] { input.Count - 1 };
            else if (target.StartsWith("/") && target.EndsWith("/"))
            {
                List<int> ret = new List<int>();

                Regex rPattern = new Regex(target.Substring(1, target.Length - 2));

                for(int i = 0; i < input.Count; i++)
                {
                    if (rPattern.IsMatch(input[i])) ret.Add(i);
                }

                return ret.ToArray();
            }
            else
            {
                int idx;

                if(int.TryParse(target, out idx))
                {
                    return new int[] { idx };
                }
            }

            return new int[] { -1 };
        }

        /// <summary>
        /// 対象の配列がnullか空なら真を返す
        /// </summary>
        /// <param name="target">対象のint配列</param>
        /// <returns>対象がnullか空か</returns>
        private bool IsNullOrEmpty(int[] target)
        {
            if (target == null) return true;
            if (target[0] == -1) return true;

            return false;
        }

        /// <summary>
        /// アドレス指定子が指し示すインデックスを取得する
        /// </summary>
        /// <param name="input">入力文字列群</param>
        /// <returns>アドレス指定子が指し示すインデックス群</returns>
        public int[] GetIndex(List<string> input)
        {
            List<int> ret = new List<int>();

            //retへの変数を一度決める
            int[] startIdx = ConvertToIndex(Start, input);
            int[] objectIdx = ConvertToIndex(Option.Object, input);

            if (IsNullOrEmpty(startIdx)) return null;

            if (Option.Verb == 0)
            {
                foreach (int s in startIdx)
                {
                    ret.Add(s);
                }
            }
            else if (IsNullOrEmpty(objectIdx)) return null;
            else if (Option.Verb == ',')
            {
                for(int i = startIdx[0]; i <= objectIdx[objectIdx.Length - 1]; i++)
                {
                    ret.Add(i);
                }
            }
            else if (Option.Verb == '~')
            {
                for (int i = startIdx[0]; i < input.Count; i += objectIdx[0])
                {
                    ret.Add(i);
                }
            }

            //Notがあるなら否定で返す。
            if (Not)
            {
                List<int> tmp = new List<int>();

                //全集合
                for (int i = 0; i < input.Count; i++)
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

        static readonly Parser<string> Text = Parse.CharExcept('/').AtLeastOnce().Text();
        static readonly Parser<string> TextAt = Parse.CharExcept('@').AtLeastOnce().Text();


        /// <summary>
        /// Addressとなる数字か＄をパースするパーサ。
        /// "100$"などは正常にパースしてしまう……。警告できない。
        /// </summary>
        static readonly Parser<string> AddressNumber = digit.XOr(Parse.String("$").Text());

        /// <summary>
        /// パターンマッチングによるアドレス指定
        /// </summary>
        static readonly Parser<string> PatternAddress =
            from slash1 in Parse.Char('/')
            from pattern in Text
            from slash2 in Parse.Char('/')
            select slash1 + pattern + slash2;

        /// <summary>
        /// アドレス指定子のオプションパーサー
        /// </summary>
        static readonly Parser<OptionAddress> OptionAddress =
            from commma in Parse.Char(',').Or(Parse.Char('~'))
            from argNum in AddressNumber.XOr(PatternAddress)
            select new OptionAddress(commma, argNum);

        /// <summary>
        /// アドレス指定子のパーサー
        /// </summary>
        static readonly Parser<Address> AddressText =
            from startAddress in AddressNumber.XOr(PatternAddress)
            from option in OptionAddress.XOr<OptionAddress>(Parse.Return(new OptionAddress()))
            from not in Parse.String("!").Text().XOr(Parse.Return(""))
            select new Address(startAddress, option, not);

        static readonly Parser<Script> ScriptText =
            from ad in AddressText
            from cmd in NoArgumentCommand.XOr(ArgumentCommand).Or(ArgumentCommandAt)
            select new Script(ad, cmd);

        /// <summary>
        /// 引数を取らないコマンドのパーサー。
        /// </summary>
        static readonly Parser<Command> NoArgumentCommand =
            from cmd in Parse.Chars("dp".ToCharArray()).End()
            select new Command(cmd);

        /// <summary>
        /// 引数を取るコマンドのパーサー。
        /// </summary>
        static readonly Parser<Command> ArgumentCommand =
            from cmd in Parse.Chars("rs".ToCharArray())
            from delimiter1 in Parse.Char('/')
            from text1 in Text
            from delimiter2 in Parse.Char('/')
            from text2 in Text.XOr(Parse.Return(""))
            from delimiter3 in Parse.Char('/')
            select new Command(cmd, text1, text2);

        /// <summary>
        /// 引数をとるコマンドのパーサー。＠版。
        /// </summary>
        static readonly Parser<Command> ArgumentCommandAt =
            from cmd in Parse.Chars("rs".ToCharArray())
            from delimiter1 in Parse.Char('@')
            from text1 in TextAt
            from delimiter2 in Parse.Char('@')
            from text2 in TextAt.XOr(Parse.Return(""))
            from delimiter3 in Parse.Char('@').End()
            select new Command(cmd, text1, text2);


        static void Main(string[] args)
        {
            ReadOnlyCollection<string> OPTION_HAS_ARG = Array.AsReadOnly(new string[] { "e" });
            List<string> path = new List<string>();
            List<string> option = new List<string>();
            List<string> script = new List<string>();
            List<string> input = new List<string>();
            List<string> patternSpace = new List<string>();

            //var test = AddressText.TryParse("1,/bbb/");
            //if (test.WasSuccessful) Console.WriteLine(test.Value);

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

                // Encoding解釈(日本語:path[0]優先)
                Encoding enc = Encoding.GetEncoding("utf-8");
                if (path.Count != 0)
                {
                    FileInfo fi = new FileInfo(path[0]);
                    using (FileReader fr = new FileReader(fi))
                    {
                        //日本語優先でエンコード判別
                        fr.ReadJEnc = ReadJEnc.JP;
                        CharCode cc = fr.Read(fi);

                        //エンコード判別に成功したならエンコードを上書き
                        var tmp = cc.GetEncoding();
                        if (tmp != null) enc = tmp;
                    }
                }

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
                bool rulerMode = false;

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

                        case "r":
                            rulerMode = true;
                            break;

                        default:
                            Console.WriteLine("{0} is illigall option! :-<", op);
                            break;
                    }
                }

                // OutputEncodingにUnicodeEncoding型を代入するとNG
                if(! (enc is UnicodeEncoding) ) Console.OutputEncoding = enc;

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
                    var scResult = ScriptText.TryParse(st);
                    if(scResult.WasSuccessful)
                    {
                        Script sc = scResult.Value;

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
                                    patternSpace[idxs[i]] = patternSpace[idxs[i]].Replace(sc.Command.TargetText, sc.Command.ReplaceText);
                                }
                                break;

                            case 'r':
                                for (int i = 0; i < idxs.Length; i++)
                                {
                                    patternSpace[idxs[i]] = new Regex(sc.Command.TargetText).Replace(patternSpace[idxs[i]], sc.Command.ReplaceText);
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }

                StringBuilder sb = new StringBuilder();
                int lineNum = 0;
                foreach (string line in patternSpace.ToArray())
                {
                    if (rulerMode)
                    {
                        sb.Append(lineNum++.ToString("D6"));
                        sb.Append(" ");
                    }

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
        /// <param name="path">入力となる指定パス</param>
        /// <param name="input">出力先</param>
        /// <param name="enc">エンコード</param>
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
        /// 標準入力と入力パスから入力文字列を抽出する。
        /// </summary>
        /// <param name="path">入力となる指定パス</param>
        /// <param name="input">出力先</param>
        /// <param name="enc">エンコード</param>
        private static void ExtractInput(List<string> path, List<string> input, UnicodeEncoding enc)
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
