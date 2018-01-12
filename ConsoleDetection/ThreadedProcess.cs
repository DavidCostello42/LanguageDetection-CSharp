using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

namespace com.deltamango.LanguageDetection.ConsoleDetection
{
    public class ThreadedProcess
    {        
        public static int iCount = 0;
        public static int iMaxCount = 0;

        public ReportThreadResultsDeletage Callback { get; set; }
        public ManualResetEvent PoolMonitor { get; set; }
        public String Text { get; set; }
        public bool VerboseMode { get; set; }

        public ThreadedProcess()
        {

        }

        public ThreadedProcess(String text)
        {
            Text = text;
        }

        // Beta is the method that will be called when the work item is
        // serviced on the thread pool.
        // That means this method will be called when the thread pool has
        // an available thread for the work item.
        public void Run(object filesObj)
        {
            Guid uid = Guid.NewGuid();
            List<String> files = (List<String>)filesObj;

            DateTime start = DateTime.UtcNow;
            
            ThreadResult threadResult = new ThreadResult();
            threadResult.ThreadId = uid.ToString();

            if (VerboseMode) { Console.WriteLine(String.Format("{0}: {1} files to process: {2}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), uid.ToString(), files.Count)); }

            foreach (String file in files)
            {
                try
                {
                    FileInfo fi = new FileInfo(file);
                    if (VerboseMode)
                    {
                        Console.WriteLine(String.Format("{0}: {1} processing: {2}",
                            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), uid.ToString(), fi.Name));
                    }

                    Lookup lookup = new Lookup(file);
                    List<double> results = new List<double>();
                    DateTime idTime = DateTime.UtcNow;
                    int score = lookup.Identify(Text);
                    results.Add(score);
                    results.Add((DateTime.UtcNow - idTime).TotalMilliseconds);
                    threadResult.Scores.Add(fi.Name, results);
                }
                catch (Exception e)
                {
                    if (VerboseMode) { Console.WriteLine(String.Format("{0}: Thread {1} experienced an exception procdessing {2}. {3}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), uid.ToString(), file, e.ToString())); }
                }
            }
            threadResult.ExecutionTime = (DateTime.UtcNow - start).TotalMilliseconds;

            if (VerboseMode) { Console.WriteLine(String.Format("{0}: {1} completed: {2}ms", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), uid.ToString(), threadResult.ExecutionTime)); }

            Callback(threadResult);

            PoolMonitor.Set();
        }

        public delegate void ReportThreadResultsDeletage(ThreadResult threadResult);
    }
}
