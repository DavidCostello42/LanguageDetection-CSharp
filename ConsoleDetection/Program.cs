using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace com.deltamango.LanguageDetection.ConsoleDetection
{
    class Program
    {
        protected static bool _stopError = false;
        protected static bool _setDirectory = false;
        protected static bool _setFile = false;
        protected static bool _setMode = false;
        protected static bool _setThreads = false;
        protected static Dictionary<String, List<double>> _scores = new Dictionary<string, List<double>>();
        protected static String _bestMatch = "";
        protected static int _bestScore = 0;
        protected static double _executionTime = 0.0;
        protected static int _maximumThreads = 1;
        protected static int _finishedThreads = 0;

        public static bool VerboseMode { get; set; }
        public static String DirectoryName { get; set; }
        public static String Text { get; set; }
        public static bool fullReport { get; set; }
        public static int OutputMode { get; set; }


        static void Main(string[] args)
        {
            // Build the directory path for the dictionaries.
            DirectoryName = String.Format("{0}\\dictionaries", AppDomain.CurrentDomain.BaseDirectory);

            Text = "";
            OutputMode = 1;
            fullReport = false;

            // If no argument are provided, show the help and stop processing.
            if (args.Length <= 0)
            {
                help();
            }
            else
            {
                // We have arguments on the command line - loop through them to enable functionality and process.

                // For argument switches that have settings (eg /t or /m), the relevant _set<Whatever> boolean is set true.
                // On the next loop, if the boolean flag is enabled then the next input value is assumed to be the setting value.
                // Once the setting value is captured the boolean is toggled so the next argument can be read.

                foreach (String arg in args)
                {
                    if (_stopError) { break; }
                    if (VerboseSwitch(arg)) { VerboseMode = true; continue; }
                    if (HelpSwitch(arg)) { _stopError = true; help(); break; } // If help flag is found, we abort arg processing.
                    if (DirectorySwitch(arg)) { _setDirectory = true; continue; }
                    if (FileSwitch(arg)) { _setFile = true; continue; }
                    if (ReportSwitch(arg)) { fullReport = true; continue; }
                    if (ModeSwitch(arg)) { _setMode = true; continue; }
                    if (ThreadsSwitch(arg)) { _setThreads = true; continue; }

                    if (_setDirectory) { SetDirectoryArgument(arg); }
                    else if (_setFile) { SetFileArgument(arg); }
                    else if (_setMode) { SetModeArgument(arg); }
                    else if (_setThreads) { SetThreadsArgument(arg); }
                    else { Text = arg; break; }
                }

                if (!_stopError)
                {
                    if (String.IsNullOrEmpty(Text)) { help(); }
                    else
                    {
                        if (_maximumThreads == 1) 
                        {
                            if (VerboseMode) { Console.WriteLine(String.Format("{0}: Single-threaded processing starting.", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))); }
                            ProcessSingleThreadedText(); 
                        }
                        else
                        {
                            if (VerboseMode) { Console.WriteLine(String.Format("{0}: Multi-threaded processing starting.", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))); }
                            ProcessMultiThreadedText();
                        }

                        
                        if (fullReport)
                        {
                            switch (OutputMode)
                            {
                                case 2:
                                    {
                                        foreach (KeyValuePair<String, List<double>> score in _scores)
                                        {
                                            Console.WriteLine(String.Format("{0},{1},{2}", score.Key, score.Value[0], score.Value[1]));
                                        }
                                    }
                                    break;
                                case 3:
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        sb.Append("{\"results\":[");
                                        int c = 0;
                                        foreach (KeyValuePair<String, List<double>> score in _scores)
                                        {
                                            if (c > 0) { sb.Append(","); }
                                            sb.AppendFormat("{{\"file\":\"{0}\",\"score\":\"{1}\",\"ms\":\"{2}\"}}", score.Key, score.Value[0], score.Value[1]);
                                            c++;
                                        }
                                        sb.AppendFormat("],\"ms\":\"{0}\"}}", _executionTime);
                                        Console.WriteLine(sb.ToString());
                                    }
                                    break;
                                case 1:
                                default:
                                    {
                                        foreach (KeyValuePair<String, List<double>> score in _scores)
                                        {
                                            Console.WriteLine(String.Format("{0}\t{1}\t{2}ms", score.Key, score.Value[0], score.Value[1]));
                                        }
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            String outputText = String.Format("Result: {0} ({1})/in: {2}ms", _bestMatch, _bestScore, _executionTime);
                            switch (OutputMode)
                            {
                                case 2:
                                    outputText = String.Format("{0},{1},{2}", _bestMatch, _bestScore, _executionTime);
                                    break;
                                case 3:
                                    outputText = String.Format("{{\"result\":{{\"match\":\"{0}\",\"score\":\"{1}\",\"ms\":\"{2}\"}}}}", _bestMatch, _bestScore, _executionTime);
                                    break;
                                case 1:
                                default:
                                    break;
                            }

                            Console.WriteLine(outputText);
                        }
                    }
                }
            }
        }

        protected static void ProcessSingleThreadedText()
        {
            // read dictionary directory
            String[] files = Directory.GetFiles(DirectoryName);

            DateTime start = DateTime.UtcNow;
            
            foreach (String file in files)
            {
                FileInfo fi = new FileInfo(file);
                Lookup lookup = new Lookup(file);
                List<double> results = new List<double>();
                DateTime idTime = DateTime.UtcNow;
                int score = lookup.Identify(Text);
                results.Add(score);
                results.Add((DateTime.UtcNow - idTime).TotalMilliseconds);
                _scores.Add(fi.Name, results);
                if (score > _bestScore)
                {
                    _bestScore = score;
                    _bestMatch = fi.Name;
                }
            }
            _executionTime = (DateTime.UtcNow - start).TotalMilliseconds;
        }

        protected static void ProcessMultiThreadedText()
        {
            try
            {
                ManualResetEvent poolMonitor = new ManualResetEvent(false);
                ThreadPool.SetMinThreads(1, 1);
                ThreadPool.SetMaxThreads(_maximumThreads, _maximumThreads);

                // read dictionary directory
                String[] files = Directory.GetFiles(DirectoryName);

                int perThread = Convert.ToInt32(Math.Ceiling((double)files.Length / _maximumThreads));
                if (VerboseMode) { Console.WriteLine(String.Format("{0}: Allocated {1} dictionary files per thread.", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), perThread)); }

                List<ThreadedProcess> processThreads = new List<ThreadedProcess>();

                int filesIndex = 0;
                for (int i = 0; i < _maximumThreads; i++)
                {
                    ThreadedProcess processThread = new ThreadedProcess(Text);

                    processThread.PoolMonitor = poolMonitor;
                    processThread.VerboseMode = VerboseMode;
                    processThread.Callback = ReportThreadResults;

                    processThreads.Add(processThread);

                    List<String> list = new List<string>();
                    for (int j = 0; j < perThread; j++)
                    {
                        if (filesIndex + j < files.Length)
                        {
                            list.Add(files[filesIndex + j]);
                        }
                    }
                    filesIndex += list.Count;
                    if (VerboseMode) { Console.WriteLine(String.Format("{0}: List {1} contains: [{2}].", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), i, String.Join(", ", list.ToArray()))); }
                    ThreadPool.QueueUserWorkItem(processThreads[i].Run, list);
                    if (VerboseMode) { Console.WriteLine(String.Format("{0}: List {1} queued.", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), i)); }
                }

                if (VerboseMode) { Console.WriteLine(String.Format("{0}: Waiting for threads to end.", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))); }
                poolMonitor.WaitOne(Timeout.Infinite, true);
                
                int k = 0;
                while (_finishedThreads < _maximumThreads)
                {
                    Thread.Sleep(100);
                    k++;
                    if (k > 100)
                    {
                        break;
                    }
                }

                if (VerboseMode) { Console.WriteLine(String.Format("{0}: Threads ended.", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))); }
                
                foreach (KeyValuePair<String, List<double>> score in _scores)
                {
                    if (score.Value[0] > _bestScore)
                    {
                        _bestScore = Convert.ToInt32(score.Value[0]);
                        _bestMatch = score.Key;
                    }
                }
            }
            catch (Exception e) {
                if (VerboseMode) { Console.WriteLine(String.Format("{0}: An exception was thrown whilst processing multiple threads. {1}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), e.ToString())); }
            }
        }

        public static void ReportThreadResults(ThreadResult threadResult)
        {
            if (VerboseMode) { Console.WriteLine(String.Format("{0}: {1} Reporting a score from thread.", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), threadResult.ThreadId)); }
            bool saved = false;
            while (!saved)
            {
                if (VerboseMode) { Console.WriteLine(String.Format("{0}: {1} Waiting for lock to write scores.", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), threadResult.ThreadId)); }
                lock (_scores)
                {
                    _executionTime += threadResult.ExecutionTime;
                    foreach (KeyValuePair<String, List<double>> score in threadResult.Scores)
                    {
                        if (VerboseMode) { Console.WriteLine(String.Format("{0}: {4} Reporting a score from {1}, score: {2} (in {3} ms).", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), score.Key, score.Value[0], score.Value[1], threadResult.ThreadId)); }
                        _scores.Add(score.Key, score.Value);
                    }
                    saved = true;
                }
            }
            _finishedThreads += 1;
        }

        protected static void SetThreadsArgument(string arg)
        {
            int maxThreads = 1;
            int.TryParse(arg, out maxThreads);
            _maximumThreads = maxThreads;
            _setThreads = false;
        }

        protected static void SetModeArgument(string arg)
        {
            switch (arg.ToLower())
            {
                case "csv":
                    OutputMode = 2;
                    break;
                case "json":
                    OutputMode = 3;
                    break;
                default:
                    break;
            }
            _setMode = false;
        }

        protected static void SetFileArgument(string arg)
        {
            try
            {
                FileInfo fi = new FileInfo(arg);
                if (File.Exists(fi.FullName))
                {
                    Text = File.ReadAllText(fi.FullName);
                }
                else
                {
                    _stopError = true;
                    Console.WriteLine("Error: File invalid. Run /help to check syntax");
                }
            }
            catch (Exception ex)
            {
                _stopError = true;
                Console.WriteLine(String.Format("Error: Exception whilst accessing file. {0}", ex.Message));
            }
            _setFile = false;
        }

        protected static void SetDirectoryArgument(string arg)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(arg);
                if (Directory.Exists(di.FullName))
                {
                    DirectoryName = di.FullName;
                }
                else
                {
                    _stopError = true;
                    Console.WriteLine("Error: Directory invalid. Run /help to check syntax");
                }
            }
            catch (Exception ex)
            {
                _stopError = true;
                Console.WriteLine(String.Format("Error: Exception whilst accessing directory. {0}", ex.Message));
            }
            _setDirectory = false;
        }

        protected static void help()
        {
            Console.WriteLine("");
            Console.WriteLine("LanguageDetection /help");
            Console.WriteLine("");
            Console.WriteLine("Parameters must be specified to use this tool.");
            Console.WriteLine("NOTE: Input text is always the last parameter.");
            Console.WriteLine("");
            //                 ===========================================================================
            Console.WriteLine("/? /h /help    Prints help.");
            Console.WriteLine("/d /dir <dir>  The directory where dictionaries are found.");
            Console.WriteLine("               Defaults to the folder idl is located when not specified.");
            Console.WriteLine("               (Remember to encase in quotes if your dir name has spaces).");
            Console.WriteLine("/f <file>      Alternative to text input - specify a text file to read.");
            Console.WriteLine("/m <mode>      Change output on console to CSV or JSON.");
            Console.WriteLine("/r             Enable full report of matches against each dictionary.");
            Console.WriteLine("/t <max>       Enable threading and set a maximum number of worker threads.");
            Console.WriteLine("\"<text>\"       The text to analyse in quotes.");  // The indentation on this line is caused by the escaped quotes.
            Console.WriteLine("");
            Console.WriteLine("Example usage:");
            Console.WriteLine("               idl \"This is a test of the language detection capability.\"");
            Console.WriteLine("               idl /d \"c:\\dictionaries\" \"Moved dictionaries here.\"");
            Console.WriteLine("               idl /f \"c:\\texts\\file.txt\"");
            Console.WriteLine("               idl /d \"c:\\dictionaries\" /f \"c:\\texts\\file.txt\"");
            Console.WriteLine("               idl /m CSV /r /t 2 \"This is example text in English.\"");
        }

        protected static bool HelpSwitch(String arg)
        {
            if (arg.Equals("/?") || arg.ToLower().Equals("/h") || arg.ToLower().Equals("/help")
                        || arg.Equals("-?") || arg.ToLower().Equals("-h") || arg.ToLower().Equals("-help")
                        || arg.Equals("--?") || arg.ToLower().Equals("--h") || arg.ToLower().Equals("--help"))
            {
                return true;
            }
            else { return false; }
        }

        protected static bool DirectorySwitch(String arg)
        {
            if (arg.ToLower().Equals("/d") || arg.ToLower().Equals("/dir") || arg.ToLower().Equals("/directory")
                        || arg.ToLower().Equals("-d") || arg.ToLower().Equals("-dir") || arg.ToLower().Equals("-directory")
                        || arg.ToLower().Equals("--d") || arg.ToLower().Equals("--dir") || arg.ToLower().Equals("--directory"))
            {
                return true;
            }
            else { return false; }
        }

        protected static bool FileSwitch(String arg)
        {
            if (arg.ToLower().Equals("/f") || arg.ToLower().Equals("/file")
                        || arg.ToLower().Equals("-f") || arg.ToLower().Equals("-file")
                        || arg.ToLower().Equals("--f") || arg.ToLower().Equals("--file"))
            {
                return true;
            }
            else { return false; }
        }

        protected static bool ReportSwitch(String arg)
        {
            if (arg.Equals("/r") || arg.ToLower().Equals("/report")
                        || arg.Equals("-r") || arg.ToLower().Equals("-report")
                        || arg.Equals("--r") || arg.ToLower().Equals("--report"))
            {
                return true;
            }
            else { return false; }
        }

        protected static bool ModeSwitch(String arg)
        {
            if (arg.Equals("/m") || arg.ToLower().Equals("/mode")
                        || arg.Equals("-m") || arg.ToLower().Equals("-mode")
                        || arg.Equals("--m") || arg.ToLower().Equals("--mode"))
            {
                return true;
            }
            else { return false; }
        }

        protected static bool ThreadsSwitch(string arg)
        {
            if (arg.Equals("/t") || arg.ToLower().Equals("/thread") || arg.ToLower().Equals("/threads") || arg.ToLower().Equals("/threading")
                        || arg.Equals("-t") || arg.ToLower().Equals("-thread") || arg.ToLower().Equals("-threads") || arg.ToLower().Equals("-threading")
                        || arg.Equals("--t") || arg.ToLower().Equals("--thread") || arg.ToLower().Equals("--threads") || arg.ToLower().Equals("--threading"))
            {
                return true;
            }
            else { return false; }
        }

        private static bool VerboseSwitch(string arg)
        {
            if (arg.Equals("/v") || arg.ToLower().Equals("/verbose") || arg.ToLower().Equals("/debug")
                        || arg.Equals("-v") || arg.ToLower().Equals("-verbose") || arg.ToLower().Equals("-debug")
                        || arg.Equals("--v") || arg.ToLower().Equals("--verbose") || arg.ToLower().Equals("--debug"))
            {
                return true;
            }
            else { return false; }
        }

    }
}
